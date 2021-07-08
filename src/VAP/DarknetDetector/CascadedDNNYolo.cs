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
using Utils.ShapeTools;
using Wrapper.Yolo;
using Wrapper.Yolo.Model;

namespace DarknetDetector
{
    public class CascadedDNNDarknet
    {
        private const string YOLOCONFIG = "YoloV3Coco"; // "cheap" yolo config folder name
        private readonly IDNNAnalyzer _frameDNNYolo;
        private readonly FrameBuffer _frameBufferCcDNN;

        public CascadedDNNDarknet(double rFactor)
        {
            _frameBufferCcDNN = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            resultCache = new Dictionary<IFrame, IList<IItemID>>();
            _frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.CC, rFactor);
        }

        public CascadedDNNDarknet(List<(string key, LineSegment coordinates)> lines)
        {
            _frameBufferCcDNN = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);
            resultCache = new Dictionary<IFrame, IList<IItemID>>();
            _frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.CC, lines);
        }

        public IList<IFramedItem> Run(IFrame frame,
                                      IList<IFramedItem> ltDNNItemList,
                                      ISet<string> category,
                                      object previousSource,
                                      object sourceObject)
        {
            if (ltDNNItemList == null)
            {
                return null;
            }

            var itemsOfInterest = GetRelevantItems(ltDNNItemList, previousSource);

            foreach (var ioi in itemsOfInterest)
            {
                Console.WriteLine("** Calling Heavy");
                var results = GetAnalysisResults(ioi.FramedItem.Frame, category, sourceObject);
                var ID = ioi.TriggeredID;
                var bbox = ID.BoundingBox;
                IItemID closestMatch = null;
                float closestiou = 0.0f;
                for (int i = 0; i < results.Count; i++)
                {
                    float iou = results[i].BoundingBox.IntersectionOverUnion(bbox);
                    if (iou > closestiou)
                    {
                        closestiou = iou;
                        closestMatch = results[i];
                    }
                }

                if (closestiou > 0.5)
                {
                    // item found
                    var newID = new ItemID(closestMatch)
                    {
                        FurtherAnalysisTriggered = true
                    };
                    ioi.FramedItem.ItemIDs.Add(newID);

                    string fileName = $@"frame-{ioi.FramedItem.Frame.FrameIndex}-Heavy-{newID.Confidence}.jpg";
                    string[] folders = new string[] { OutputFolder.OutputFolderCcDNN, OutputFolder.OutputFolderAll };
                    Utils.Utils.SaveFoundItemImage(folders, fileName, ioi.FramedItem, ioi.FramedItem.ItemIDs.Count - 1, Color.Red);
                }
                else
                {
                    // item now found by heavy
                    Console.WriteLine("**Not detected by Heavy");
                }
            }

            /*for (int i = 0; i < ltDNNItemList.Count; i++)
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
                    IEnumerable<IItemID> analyzedTrackingItems = null;
                    Mat imgByte = ltDNNItem.Frame.FrameData;
                    int realFrameIndex = ltDNNItem.Frame.FrameIndex;

                    Console.WriteLine("** Calling Heavy");

                    if (ltDNNID is LineTriggeredItemID lineTriggered)
                    {
                        //_frameDNNYolo.SetTrackingPoint(lines[lineTriggered.TriggerLineID].coordinates.MidPoint); //only needs to check the last line in each row
                        //analyzedTrackingItems = _frameDNNYolo.AnalyzeAndDetect(imgByte,
                        //                                                       category,
                        //                                                       lineTriggered.TriggerLineID,
                        //                                                       System.Drawing.Color.Red,
                        //                                                       DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL,
                        //                                                       lines[lineTriggered.TriggerLineID].coordinates.MidPoint,
                        //                                                       realFrameIndex);
                        analyzedTrackingItems = _frameDNNYolo.Analyze(imgByte, category, sourceObject);
                    }
                    else
                    {
                        //return _frameDNNYolo.Run(imgByte, realFrameIndex, lines, category, System.Drawing.Color.Red);
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
            }*/

            return ltDNNItemList;
        }

        private IEnumerable<(IFramedItem FramedItem, IItemID TriggeredID)> GetRelevantItems(IList<IFramedItem> ltDNNItemList, object previousSource)
        {
            for (int i = 0; i < ltDNNItemList.Count; i++)
            {
                var fi = ltDNNItemList[i];
                for (int j = 0; j < ltDNNItemList[i].ItemIDs.Count; j++)
                {
                    var id = fi.ItemIDs[j];
                    if (id.FurtherAnalysisTriggered && id.SourceObject == previousSource && id.Confidence < DNNConfig.CONFIDENCE_THRESHOLD)
                    {
                        yield return (fi, id);
                        break;
                    }
                }
            }
        }

        private IItemID GetRelevantID(IFramedItem framedItem, object previousSource)
        {
            for (int j = framedItem.ItemIDs.Count - 1; j > 0; --j)
            {
                var id = framedItem.ItemIDs[j];
                if (id.FurtherAnalysisTriggered && id.SourceObject == previousSource && id.Confidence < DNNConfig.CONFIDENCE_THRESHOLD)
                {
                    return id;
                }
            }
            return null;
        }

        /*public IList<IFramedItem> Run(IFrame frame, IList<IFramedItem> ltDNNItemList, IDictionary<string, LineSegment> lines, ISet<string> category, object sourceObject = null)
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
                                SourceObject = sourceObject,
                                FurtherAnalysisTriggered = true
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
        }*/

        private IList<IItemID> GetAnalysisResults(IFrame frame, ISet<string> category, object sourceObject)
        {
            if (!resultCache.ContainsKey(frame))
            {
                var result = _frameDNNYolo.Analyze(frame.FrameData, category, sourceObject);
                if (result is IList<IItemID> list)
                {
                    resultCache.Add(frame, list);
                    return list;
                }
                else
                {
                    var newlist = new List<IItemID>(result);
                    resultCache.Add(frame, new List<IItemID>(result));
                    return newlist;
                }
            }
            return resultCache[frame];
        }

        private static Item Item(YoloTrackingItem yoloTrackingItem)
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

        private IDictionary<IFrame, IList<IItemID>> resultCache;
    }
}
