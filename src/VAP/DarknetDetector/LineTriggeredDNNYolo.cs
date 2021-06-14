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
        private const string YOLOCONFIG = "YoloV3TinyCoco"; // "cheap" yolo config folder name
        private readonly FrameDNNDarknet _frameDNNYolo;
        private readonly FrameBuffer _frameBufferLtDNNYolo;
        private readonly Dictionary<string, int> _counts_prev = new Dictionary<string, int>();

        public LineTriggeredDNNDarknet(double rFactor)
        {
            _frameBufferLtDNNYolo = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            _frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.LT, rFactor);
        }

        public LineTriggeredDNNDarknet(List<(string key, LineSegment coordinates)> lines)
        {
            _frameBufferLtDNNYolo = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            _frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.LT, lines);
        }

        public IList<IFramedItem> Run(IFrame frame, Dictionary<string, int> counts, List<(string key, LineSegment coordinates)> lines, ISet<string> category, IList<IFramedItem> items, object sourceObject = null)
        {
            // buffer frame
            _frameBufferLtDNNYolo.Buffer(frame.FrameData);
            int frameIndex = frame.FrameIndex;
            if (_counts_prev.Count != 0)
            {
                foreach (IFramedItem it in items)
                {
                    bool triggered = false;
                    ILineTriggeredItemID id = null;
                    for (int i = it.ItemIDs.Count - 1; i >= 0; --i)
                    {
                        if (it.ItemIDs[i] is ILineTriggeredItemID trig && trig.FurtherAnalysisTriggered)
                        {
                            id = trig;
                            triggered = true;
                        }
                    }

                    if (triggered) //object detected by BGS-based counter
                    {
                        if (frameIndex >= DNNConfig.FRAME_SEARCH_RANGE)
                        {
                            // call yolo for crosscheck
                            int lineID = Array.IndexOf(counts.Keys.ToArray(), id.TriggerLine);
                            _frameDNNYolo.SetTrackingPoint(lines[lineID].coordinates.MidPoint); //only needs to check the last line in each row
                            //Mat[] frameBufferArray = _frameBufferLtDNNYolo.ToArray();
                            int frameIndexYolo = frameIndex - 1;
                            DateTime start = DateTime.Now;
                            List<YoloTrackingItem> analyzedTrackingItems = null;

                            while (frameIndex - frameIndexYolo < DNNConfig.FRAME_SEARCH_RANGE)
                            {
                                Console.WriteLine("** Calling Cheap on " + (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)));
                                Mat frameYolo = _frameBufferLtDNNYolo[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)];
                                // byte[] imgByte = Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameYolo));

                                analyzedTrackingItems = _frameDNNYolo.Detect(frameYolo, category, lineID, System.Drawing.Color.Pink, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE, frameIndexYolo);

                                // object detected by cheap YOLO
                                if (analyzedTrackingItems != null)
                                {
                                    foreach (YoloTrackingItem yoloTrackingItem in analyzedTrackingItems)
                                    {
                                        Rectangle bounds = new Rectangle(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height);
                                        LineTriggeredItemID itemID = new LineTriggeredItemID(bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.TrackId, nameof(LineTriggeredDNNDarknet))
                                        {
                                            TriggerLine = lines[lineID].key,
                                            TriggerLineID = lineID,
                                            SourceObject = sourceObject,
                                            FurtherAnalysisTriggered = true
                                        };
                                        if (itemID.InsertIntoFramedItemList(items, out IFramedItem framedItem, frameIndexYolo))
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
                                    UpdateCount(counts);
                                    return items;
                                }
                                frameIndexYolo--;
                                frameIndexYolo--;
                            }
                        }
                    }
                }
            }
            UpdateCount(counts);

            return items;
        }

        public IList<IFramedItem> Run(Mat frame, int frameIndex, Dictionary<string, int> counts, List<(string key, LineSegment coordinates)> lines, ISet<string> category, IList<IFramedItem> items, object sourceObject = null)
        {
            // buffer frame
            _frameBufferLtDNNYolo.Buffer(frame);

            if (_counts_prev.Count != 0)
            {
                foreach (string lane in counts.Keys)
                {
                    int diff = Math.Abs(counts[lane] - _counts_prev[lane]);
                    if (diff > 0) //object detected by BGS-based counter
                    {
                        if (frameIndex >= DNNConfig.FRAME_SEARCH_RANGE)
                        {
                            // call yolo for crosscheck
                            int lineID = Array.IndexOf(counts.Keys.ToArray(), lane);
                            _frameDNNYolo.SetTrackingPoint(lines[lineID].coordinates.MidPoint); //only needs to check the last line in each row
                            //Mat[] frameBufferArray = _frameBufferLtDNNYolo.ToArray();
                            int frameIndexYolo = frameIndex - 1;
                            DateTime start = DateTime.Now;
                            List<YoloTrackingItem> analyzedTrackingItems = null;

                            while (frameIndex - frameIndexYolo < DNNConfig.FRAME_SEARCH_RANGE)
                            {
                                Console.WriteLine("** Calling Cheap on " + (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)));
                                Mat frameYolo = _frameBufferLtDNNYolo[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)];
                                // byte[] imgByte = Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameYolo));

                                analyzedTrackingItems = _frameDNNYolo.Detect(frameYolo, category, lineID, System.Drawing.Color.Pink, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE, frameIndexYolo);

                                // object detected by cheap YOLO
                                if (analyzedTrackingItems != null)
                                {
                                    foreach (YoloTrackingItem yoloTrackingItem in analyzedTrackingItems)
                                    {
                                        Rectangle bounds = new Rectangle(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height);
                                        LineTriggeredItemID item = new LineTriggeredItemID(bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.TrackId, nameof(LineTriggeredDNNDarknet))
                                        {
                                            TriggerLine = lines[lineID].key,
                                            TriggerLineID = lineID,
                                            SourceObject = sourceObject
                                        };
                                        if (item.InsertIntoFramedItemList(items, out IFramedItem framedItem, frameIndexYolo))
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
                                    UpdateCount(counts);
                                    return items;
                                }
                                frameIndexYolo--;
                            }
                        }
                    }
                }
            }
            UpdateCount(counts);

            return items;
        }

        private Item Item(YoloTrackingItem yoloTrackingItem)
        {
            Item item = new Item(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height,
                yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, 0, "")
            {
                TrackID = yoloTrackingItem.TrackId,
                Index = yoloTrackingItem.Index,
                TaggedImageData = yoloTrackingItem.TaggedImageData,
                CroppedImageData = yoloTrackingItem.CroppedImageData
            };

            return item;
        }

        private void UpdateCount(Dictionary<string, int> counts)
        {
            foreach (string dir in counts.Keys)
            {
                _counts_prev[dir] = counts[dir];
            }
        }
    }
}
