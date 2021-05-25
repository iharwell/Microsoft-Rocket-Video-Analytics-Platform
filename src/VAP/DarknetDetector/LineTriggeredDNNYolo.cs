// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Model;
using DNNDetector.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using OpenCvSharp;
using Utils.Config;
using Wrapper.Yolo;
using Wrapper.Yolo.Model;
using Utils;
using Utils.Items;
using System.Drawing;

namespace DarknetDetector
{
    public class LineTriggeredDNNDarknet
    {
        static string YOLOCONFIG = "YoloV3TinyCoco"; // "cheap" yolo config folder name
        FrameDNNDarknet frameDNNYolo;
        FrameBuffer frameBufferLtDNNYolo;
        Dictionary<string, int> counts_prev = new Dictionary<string, int>();

        public LineTriggeredDNNDarknet(double rFactor)
        {
            frameBufferLtDNNYolo = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.LT, rFactor);
        }

        public LineTriggeredDNNDarknet(List<(string key, LineSegment coordinates)> lines)
        {
            frameBufferLtDNNYolo = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.LT, lines);
        }

        public IList<IFramedItem> Run(Mat frame, int frameIndex, Dictionary<string, int> counts, List<(string key, LineSegment coordinates)> lines, HashSet<string> category, IList<IFramedItem> items)
        {
            // buffer frame
            frameBufferLtDNNYolo.Buffer(frame);

            if (counts_prev.Count != 0)
            {
                foreach (string lane in counts.Keys)
                {
                    int diff = Math.Abs(counts[lane] - counts_prev[lane]);
                    if (diff > 0) //object detected by BGS-based counter
                    {
                        if (frameIndex >= DNNConfig.FRAME_SEARCH_RANGE)
                        {
                            // call yolo for crosscheck
                            int lineID = Array.IndexOf(counts.Keys.ToArray(), lane);
                            frameDNNYolo.SetTrackingPoint( lines[lineID].coordinates.MidPoint ); //only needs to check the last line in each row
                            Mat[] frameBufferArray = frameBufferLtDNNYolo.ToArray();
                            int frameIndexYolo = frameIndex - 1;
                            DateTime start = DateTime.Now;
                            List<YoloTrackingItem> analyzedTrackingItems = null;

                            while (frameIndex - frameIndexYolo < DNNConfig.FRAME_SEARCH_RANGE)
                            {
                                Console.WriteLine("** Calling Cheap on " + (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)));
                                Mat frameYolo = frameBufferArray[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)];
                                byte[] imgByte = Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameYolo));

                                analyzedTrackingItems = frameDNNYolo.Detect(imgByte, category, lineID, System.Drawing.Brushes.Pink, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE, frameIndexYolo);

                                // object detected by cheap YOLO
                                if (analyzedTrackingItems != null)
                                {
                                    foreach (YoloTrackingItem yoloTrackingItem in analyzedTrackingItems)
                                    {
                                        Rectangle bounds = new Rectangle( yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height );
                                        LineTriggeredItemID item = new LineTriggeredItemID (bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.TrackId, nameof(FrameDNNDarknet) );
                                        item.TriggerLine = lines[lineID].key;
                                        item.TriggerLineID = lineID;

                                        if ( item.InsertIntoFramedItemList( items, out IFramedItem framedItem, frameIndexYolo ) )
                                        {
                                            var f = framedItem.Frame;
                                            f.FrameIndex = frameIndexYolo;
                                            f.SourceName = null;
                                            f.FrameData = imgByte;
                                        }

                                        // output cheap YOLO results
                                        string blobName_Cheap = $@"frame-{frameIndexYolo}-Cheap-{yoloTrackingItem.Confidence}.jpg";
                                        string fileName_Cheap = @OutputFolder.OutputFolderLtDNN + blobName_Cheap;
                                        File.WriteAllBytes(fileName_Cheap, yoloTrackingItem.TaggedImageData);
                                        File.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_Cheap, yoloTrackingItem.TaggedImageData);
                                    }
                                    updateCount(counts);
                                    return items;
                                }
                                frameIndexYolo--;
                            }
                        }
                    }
                }
            }
            updateCount(counts);

            return items;
        }

        Item Item(YoloTrackingItem yoloTrackingItem)
        {
            Item item = new Item(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height,
                yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, 0, "");

            item.TrackID = yoloTrackingItem.TrackId;
            item.Index = yoloTrackingItem.Index;
            item.TaggedImageData = yoloTrackingItem.TaggedImageData;
            item.CroppedImageData = yoloTrackingItem.CroppedImageData;

            return item;
        }

        void updateCount(Dictionary<string, int> counts)
        {
            foreach (string dir in counts.Keys)
            {
                counts_prev[dir] = counts[dir];
            }
        }
    }
}
