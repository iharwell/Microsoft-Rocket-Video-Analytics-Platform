// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Config;
using DNNDetector.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utils.Config;
using Utils;
using Utils.Items;
using System.Drawing;

namespace TFDetector
{
    public class LineTriggeredDNNTF
    {
        //static string TFCONFIG = "";
        private readonly FrameDNNTF _frameDNNTF;
        private readonly FrameBuffer _frameBufferLtDNNTF;
        private readonly Dictionary<string, int> _counts_prev = new();

        public LineTriggeredDNNTF(List<(string key, LineSegment coordinates)> lines)
        {
            _frameBufferLtDNNTF = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            _frameDNNTF = new FrameDNNTF(lines);
        }

        public IList<IFramedItem> Run(Mat frame, int frameIndex, Dictionary<string, int> counts, List<(string key, LineSegment coordinates)> lines, HashSet<string> category, IList<IFramedItem> items)
        {
            // buffer frame
            _frameBufferLtDNNTF.Buffer(frame);

            if (_counts_prev.Count != 0)
            {
                foreach (string lane in counts.Keys)
                {
                    int diff = Math.Abs(counts[lane] - _counts_prev[lane]);
                    if (diff > 0) //object detected by BGS-based counter
                    {
                        if (frameIndex >= DNNConfig.FRAME_SEARCH_RANGE)
                        {
                            // call tf cheap model for crosscheck
                            int lineID = Array.IndexOf(counts.Keys.ToArray(), lane);
                            Mat[] frameBufferArray = _frameBufferLtDNNTF.ToArray();
                            int frameIndexTF = frameIndex - 1;
                            DateTime start = DateTime.Now;
                            IList<IFramedItem> analyzedTrackingItems = null;

                            while (frameIndex - frameIndexTF < DNNConfig.FRAME_SEARCH_RANGE)
                            {
                                Console.WriteLine("** Calling Cheap on " + (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexTF)));
                                Mat frameTF = frameBufferArray[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexTF)];

                                analyzedTrackingItems = _frameDNNTF.Run(frameTF, frameIndexTF, category, System.Drawing.Color.Pink, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE, false);

                                // object detected by cheap model
                                if (analyzedTrackingItems != null)
                                {
                                    foreach (IFramedItem framedItemPre in analyzedTrackingItems)
                                    {
                                        IItemID item = framedItemPre.ItemIDs[0];
                                        LineTriggeredItemID item2 = new LineTriggeredItemID(item.BoundingBox, item.ObjectID, item.ObjName, item.Confidence, item.TrackID, nameof(FrameDNNTF))
                                        {
                                            TriggerLine = lane,
                                            TriggerLineID = lineID
                                        };
                                        if (item2.InsertIntoFramedItemList(items, out IFramedItem framedItem, frameIndexTF))
                                        {
                                            framedItem.Frame.FrameData = frameTF;
                                            framedItem.Frame.FrameIndex = frameIndexTF;
                                        }

                                        // output cheap TF results
                                        string blobName_Cheap = $@"frame-{frameIndex}-Cheap-{item.Confidence}.jpg";
                                        string fileName_Cheap = @OutputFolder.OutputFolderLtDNN + blobName_Cheap;
                                        var tagged = framedItem.TaggedImageData(framedItem.ItemIDs.Count - 1, System.Drawing.Color.Pink);
                                        Utils.Utils.WriteAllBytes(fileName_Cheap, tagged);
                                        Utils.Utils.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_Cheap, tagged);
                                    }
                                    UpdateCount(counts);
                                    return items;
                                }
                                frameIndexTF--;
                            }
                        }
                    }
                }
            }
            UpdateCount(counts);

            return items;
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
