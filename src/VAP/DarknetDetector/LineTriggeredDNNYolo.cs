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
        private static string YOLOCONFIG = "YoloV3TinyCoco"; // "cheap" yolo config folder name
        private FrameDNNDarknet frameDNNYolo;
        private FrameBuffer frameBufferLtDNNYolo;
        private Dictionary<string, int> counts_prev = new Dictionary<string, int>();

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

        public IList<IFramedItem> Run( IFrame frame, Dictionary<string, int> counts, List<(string key, LineSegment coordinates)> lines, ISet<string> category, IList<IFramedItem> items, object sourceObject = null )
        {
            // buffer frame
            frameBufferLtDNNYolo.Buffer( frame.FrameData );
            int frameIndex = frame.FrameIndex;
            if ( counts_prev.Count != 0 )
            {
                foreach ( IFramedItem it in items )
                {
                    bool triggered = false;
                    ILineTriggeredItemID id = null;
                    for ( int i = it.ItemIDs.Count-1; i >= 0; --i )
                    {
                        if( it.ItemIDs[i] is ILineTriggeredItemID trig && trig.FurtherAnalysisTriggered )
                        {
                            id = trig;
                            triggered = true;
                        }
                    }

                    if ( triggered ) //object detected by BGS-based counter
                    {
                        if ( frameIndex >= DNNConfig.FRAME_SEARCH_RANGE )
                        {
                            // call yolo for crosscheck
                            int lineID = Array.IndexOf(counts.Keys.ToArray(), id.TriggerLine);
                            frameDNNYolo.SetTrackingPoint( lines[lineID].coordinates.MidPoint ); //only needs to check the last line in each row
                            Mat[] frameBufferArray = frameBufferLtDNNYolo.ToArray();
                            int frameIndexYolo = frameIndex - 1;
                            DateTime start = DateTime.Now;
                            List<YoloTrackingItem> analyzedTrackingItems = null;

                            while ( frameIndex - frameIndexYolo < DNNConfig.FRAME_SEARCH_RANGE )
                            {
                                Console.WriteLine( "** Calling Cheap on " + ( DNNConfig.FRAME_SEARCH_RANGE - ( frameIndex - frameIndexYolo ) ) );
                                Mat frameYolo = frameBufferArray[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)];
                                // byte[] imgByte = Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameYolo));

                                analyzedTrackingItems = frameDNNYolo.Detect( frameYolo, category, lineID, System.Drawing.Color.Pink, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE, frameIndexYolo );

                                // object detected by cheap YOLO
                                if ( analyzedTrackingItems != null )
                                {
                                    foreach ( YoloTrackingItem yoloTrackingItem in analyzedTrackingItems )
                                    {
                                        Rectangle bounds = new Rectangle( yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height );
                                        LineTriggeredItemID itemID = new LineTriggeredItemID (bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.TrackId, nameof(LineTriggeredDNNDarknet) );
                                        itemID.TriggerLine = lines[lineID].key;
                                        itemID.TriggerLineID = lineID;
                                        itemID.SourceObject = sourceObject;
                                        if ( itemID.InsertIntoFramedItemList( items, out IFramedItem framedItem, frameIndexYolo ) )
                                        {
                                            var f = framedItem.Frame;
                                            f.FrameIndex = frameIndexYolo;
                                            f.SourceName = null;
                                            f.FrameData = frameYolo;
                                        }

                                        // output cheap YOLO results
                                        string blobName_Cheap = $@"frame-{frameIndexYolo}-Cheap-{yoloTrackingItem.Confidence}.jpg";
                                        string fileName_Cheap = @OutputFolder.OutputFolderLtDNN + blobName_Cheap;
                                        File.WriteAllBytes( fileName_Cheap, yoloTrackingItem.TaggedImageData );
                                        File.WriteAllBytes( @OutputFolder.OutputFolderAll + blobName_Cheap, yoloTrackingItem.TaggedImageData );
                                    }
                                    updateCount( counts );
                                    return items;
                                }
                                frameIndexYolo--;
                            }
                        }
                    }
                }
            }
            updateCount( counts );

            return items;
        }

        public IList<IFramedItem> Run( Mat frame, int frameIndex, Dictionary<string, int> counts, List<(string key, LineSegment coordinates)> lines, ISet<string> category, IList<IFramedItem> items, object sourceObject = null )
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
                            frameDNNYolo.SetTrackingPoint(lines[lineID].coordinates.MidPoint); //only needs to check the last line in each row
                            Mat[] frameBufferArray = frameBufferLtDNNYolo.ToArray();
                            int frameIndexYolo = frameIndex - 1;
                            DateTime start = DateTime.Now;
                            List<YoloTrackingItem> analyzedTrackingItems = null;

                            while (frameIndex - frameIndexYolo < DNNConfig.FRAME_SEARCH_RANGE)
                            {
                                Console.WriteLine("** Calling Cheap on " + (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)));
                                Mat frameYolo = frameBufferArray[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)];
                                // byte[] imgByte = Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameYolo));

                                analyzedTrackingItems = frameDNNYolo.Detect(frameYolo, category, lineID, System.Drawing.Color.Pink, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE, frameIndexYolo);

                                // object detected by cheap YOLO
                                if (analyzedTrackingItems != null)
                                {
                                    foreach (YoloTrackingItem yoloTrackingItem in analyzedTrackingItems)
                                    {
                                        Rectangle bounds = new Rectangle( yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height );
                                        LineTriggeredItemID item = new LineTriggeredItemID (bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.TrackId, nameof(LineTriggeredDNNDarknet) );
                                        item.TriggerLine = lines[lineID].key;
                                        item.TriggerLineID = lineID;
                                        item.SourceObject = sourceObject;
                                        if ( item.InsertIntoFramedItemList( items, out IFramedItem framedItem, frameIndexYolo ) )
                                        {
                                            var f = framedItem.Frame;
                                            f.FrameIndex = frameIndexYolo;
                                            f.SourceName = null;
                                            f.FrameData = frameYolo;
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

        private Item Item(YoloTrackingItem yoloTrackingItem)
        {
            Item item = new Item(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height,
                yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, 0, "");

            item.TrackID = yoloTrackingItem.TrackId;
            item.Index = yoloTrackingItem.Index;
            item.TaggedImageData = yoloTrackingItem.TaggedImageData;
            item.CroppedImageData = yoloTrackingItem.CroppedImageData;

            return item;
        }

        private void updateCount(Dictionary<string, int> counts)
        {
            foreach (string dir in counts.Keys)
            {
                counts_prev[dir] = counts[dir];
            }
        }
    }
}
