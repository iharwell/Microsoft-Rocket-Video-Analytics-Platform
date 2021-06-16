// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Config;
using DNNDetector.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Utils;
using Utils.Config;
using Utils.Items;
using Wrapper.Yolo;
using Wrapper.Yolo.Model;

namespace DarknetDetector
{
    public class CascadedDNNDarknet
    {
        private const string YOLOCONFIG = "YoloV3Coco"; // "cheap" yolo config folder name
        private readonly FrameDNNDarknet _frameDNNYolo;
        private readonly FrameBuffer _frameBufferCcDNN;

        public CascadedDNNDarknet(double rFactor)
        {
            _frameBufferCcDNN = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            _frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.CC, rFactor);
        }

        public CascadedDNNDarknet(List<(string key, LineSegment coordinates)> lines)
        {
            _frameBufferCcDNN = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            _frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.CC, lines);
        }

        public IList<IFramedItem> Run(Mat frame, int frameIndex, IList<IFramedItem> ltDNNItemList, List<(string key, LineSegment coordinates)> lines, HashSet<string> category)
        {
            if (ltDNNItemList == null)
            {
                return null;
            }

            for (int i = 0; i < ltDNNItemList.Count; i++)
            {
                IFramedItem ltDNNItem = ltDNNItemList[i];
                var ltDNNID = ltDNNItem.ItemIDs.Last();
                if (ltDNNID.Confidence >= DNNConfig.CONFIDENCE_THRESHOLD)
                {
                    continue;
                }
                if (!(ltDNNID is LineTriggeredItemID trigID && trigID.TriggerLine != null))
                {
                    continue;
                }
                if (ltDNNID.Confidence == 0)
                {
                    continue;
                }
                else
                {
                    Debug.Assert(ltDNNID is LineTriggeredItemID);
                    List<YoloTrackingItem> analyzedTrackingItems = null;
                    Mat imgByte = ltDNNItem.Frame.FrameData;
                    int realFrameIndex = ltDNNItem.Frame.FrameIndex;

                    Console.WriteLine("** Calling Heavy");

                    if (ltDNNID is LineTriggeredItemID lineTriggered)
                    {
                        //_frameDNNYolo.SetTrackingPoint(lines[lineTriggered.TriggerLineID].coordinates.MidPoint); //only needs to check the last line in each row
                        analyzedTrackingItems = _frameDNNYolo.AnalyzeAndDetect(imgByte,
                                                                     category,
                                                                     lineTriggered.TriggerLineID,
                                                                     System.Drawing.Color.Red,
                                                                     DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL,
                                                                     lines[lineTriggered.TriggerLineID].coordinates.MidPoint,
                                                                     realFrameIndex);
                    }
                    else
                    {
                        return _frameDNNYolo.Run(imgByte, realFrameIndex, lines, category, System.Drawing.Color.Red);
                    }

                    // object detected by heavy YOLO
                    if (analyzedTrackingItems != null)
                    {
                        foreach (YoloTrackingItem yoloTrackingItem in analyzedTrackingItems)
                        {
                            Rectangle bounds = new Rectangle(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height);
                            ItemID itemID = new ItemID(bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.Index, nameof(CascadedDNNDarknet));
                            ltDNNItem.ItemIDs.Add(itemID);
                            // output heavy YOLO results
                            string blobName_Heavy = $@"frame-{realFrameIndex}-Heavy-{yoloTrackingItem.Confidence}.jpg";
                            string fileName_Heavy = @OutputFolder.OutputFolderCcDNN + blobName_Heavy;
                            var imgData = ltDNNItem.TaggedImageData(ltDNNItem.ItemIDs.Count - 1, Color.Red);
                            Utils.Utils.WriteAllBytes(fileName_Heavy, imgData);
                            Utils.Utils.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_Heavy, imgData);

                        }
                        return ltDNNItemList; // if we only return the closest object detected by heavy model
                    }
                    else
                    {
                        Console.WriteLine("**Not detected by Heavy");
                    }
                }
            }

            return ltDNNItemList;
        }
        public IList<IFramedItem> Run(IFrame frame, IList<IFramedItem> ltDNNItemList, IDictionary<string, LineSegment> lines, ISet<string> category, object sourceObject = null)
        {
            if (ltDNNItemList == null)
            {
                return null;
            }

            for (int i = 0; i < ltDNNItemList.Count; i++)
            {
                IFramedItem ltDNNItem = ltDNNItemList[i];
                var ltDNNID = ltDNNItem.ItemIDs.Last();
                if (ltDNNID.Confidence == 0 || ltDNNID.Confidence >= DNNConfig.CONFIDENCE_THRESHOLD)
                {
                    continue;
                }
                if (!ltDNNID.FurtherAnalysisTriggered)
                {
                    continue;
                }
                if (!(ltDNNID is ILineTriggeredItemID trigID && trigID.TriggerLine != null))
                {
                    continue;
                }
                else
                {
                    Debug.Assert(ltDNNID is LineTriggeredItemID);
                    List<YoloTrackingItem> analyzedTrackingItems = null;
                    Mat imgByte = ltDNNItem.Frame.FrameData;
                    int realFrameIndex = ltDNNItem.Frame.FrameIndex;

                    Console.WriteLine("** Calling Heavy");

                    if (ltDNNID is LineTriggeredItemID lineTriggered)
                    {
                        //_frameDNNYolo.SetTrackingPoint(lines[lineTriggered.TriggerLine].MidPoint); //only needs to check the last line in each row
                        analyzedTrackingItems = _frameDNNYolo.AnalyzeAndDetect(imgByte,
                                                                     category,
                                                                     lineTriggered.TriggerLineID,
                                                                     Color.Red,
                                                                     DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL,
                                                                     lines[lineTriggered.TriggerLine].MidPoint,
                                                                     realFrameIndex);
                    }
                    else
                    {
                        return _frameDNNYolo.Run(frame, lines, category, Color.Red);
                    }

                    // object detected by heavy YOLO
                    if (analyzedTrackingItems != null)
                    {
                        foreach (YoloTrackingItem yoloTrackingItem in analyzedTrackingItems)
                        {
                            Rectangle bounds = new Rectangle(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height);
                            ItemID itemID = new ItemID(bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.Index, nameof(CascadedDNNDarknet))
                            {
                                SourceObject = sourceObject
                            };

                            ltDNNItem.ItemIDs.Add(itemID);

                            // output heavy YOLO results
                            string blobName_Heavy = $@"frame-{realFrameIndex}-Heavy-{yoloTrackingItem.Confidence}.jpg";
                            string fileName_Heavy = @OutputFolder.OutputFolderCcDNN + blobName_Heavy;
                            var imgData = ltDNNItem.TaggedImageData(ltDNNItem.ItemIDs.Count - 1, Color.Red);
                            Utils.Utils.WriteAllBytes(fileName_Heavy, imgData);
                            Utils.Utils.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_Heavy, imgData);

                        }
                        return ltDNNItemList; // if we only return the closest object detected by heavy model
                    }
                    else
                    {
                        Console.WriteLine("**Not detected by Heavy");
                    }
                }
            }

            return ltDNNItemList;
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
    }
}
