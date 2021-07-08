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
using DarknetAAB;

namespace DarknetDetector
{
    public class LineTriggeredDNNDarknet
    {
        private const string YOLOCONFIG = "YoloV3TinyCoco"; // "cheap" yolo config folder name
        private readonly IDNNAnalyzer _frameDNNYolo;
        private readonly FrameBuffer _frameBufferLtDNNYolo;
        private readonly Dictionary<string, int> _counts_prev = new();
        private readonly Dictionary<int, IEnumerable<IItemID>> _cached_dnn_outputs = new();

        private readonly List<(string key, LineSegment coordinates)> _lines;
        private IIndexChooser _indexChooser;

        private static string[] saveFolders = new string[] { OutputFolder.OutputFolderAll, OutputFolder.OutputFolderLtDNN };

        public bool DisplayFrame
        {
            get;
            set;
        }
        public IIndexChooser IndexChooser
        {
            get => _indexChooser;
            set
            {
                _indexChooser = value;
                _indexChooser.BufferDepth = DNNConfig.FRAME_SEARCH_RANGE;
            }
        }

        public LineTriggeredDNNDarknet(double rFactor)
        {
            _frameBufferLtDNNYolo = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            _frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.LT, rFactor);

            IndexChooser = new SimpleIndexChooser(3)
            {
                BufferDepth = DNNConfig.FRAME_SEARCH_RANGE
            };
        }

        public LineTriggeredDNNDarknet(List<(string key, LineSegment coordinates)> lines)
            : this(lines, new SimpleIndexChooser(3))
        { }

        public LineTriggeredDNNDarknet(List<(string key, LineSegment coordinates)> lines, IIndexChooser indexChooser)
            : this(lines, indexChooser, new Yolo4Full(0))
        {
        }

        public LineTriggeredDNNDarknet(List<(string key, LineSegment coordinates)> lines, IIndexChooser indexChooser, IDNNAnalyzer analyzer)
        {
            _frameBufferLtDNNYolo = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);
            _lines = lines;
            //_frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.LT, lines);

            IndexChooser = indexChooser;
            IndexChooser.BufferDepth = DNNConfig.FRAME_SEARCH_RANGE;
            _frameDNNYolo = analyzer;
        }

        public IList<IFramedItem> RunOld(IFrame frame, Dictionary<string, int> counts, List<(string key, LineSegment coordinates)> lines, ISet<string> category, IList<IFramedItem> items, object sourceObject = null)
        {
            // buffer frame
            _frameBufferLtDNNYolo.Buffer(frame);
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
                            //_frameDNNYolo.SetTrackingPoint(lines[lineID].coordinates.MidPoint); //only needs to check the last line in each row
                            //Mat[] frameBufferArray = _frameBufferLtDNNYolo.ToArray();
                            int frameIndexYolo = frameIndex - 1;
                            DateTime start = DateTime.Now;
                            List<IItemID> analyzedTrackingItems = null;

                            while (frameIndex - frameIndexYolo < DNNConfig.FRAME_SEARCH_RANGE)
                            {
                                Console.WriteLine("** Calling Cheap on " + (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)));
                                if (!_cached_dnn_outputs.ContainsKey(frameIndexYolo) && _cached_dnn_outputs.ContainsKey(frameIndexYolo - 1))
                                {
                                    --frameIndexYolo;
                                }
                                IFrame frameYolo = _frameBufferLtDNNYolo[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)];
                                // byte[] imgByte = Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameYolo));
                                var analyzedItemCache = GetDnnResults(frameYolo.FrameData, category, frameIndexYolo, sourceObject, Color.Pink);
                                var itemOverlaps = LineOverlapFilter.GetItemOverlap(analyzedItemCache, (IItemID id) => new RectangleF(id.BoundingBox.X, id.BoundingBox.Y, id.BoundingBox.Width, id.BoundingBox.Height), _lines[lineID].coordinates);

                                if (itemOverlaps.Count > 0)
                                {
                                    var filteredItems = from itemOverlap in itemOverlaps
                                                        where itemOverlap.Value >= DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE
                                                        orderby itemOverlap.Value descending
                                                        select itemOverlap.Key;
                                    if (filteredItems == null || !filteredItems.Any())
                                    {
                                        analyzedTrackingItems = null;
                                    }
                                    else
                                    {
                                        analyzedTrackingItems = new List<IItemID>(filteredItems);
                                    }
                                }
                                /*analyzedTrackingItems = _frameDNNYolo.Detect(frameYolo,
                                                                             category,
                                                                             lineID,
                                                                             System.Drawing.Color.Pink,
                                                                             DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE,
                                                                             lines[lineID].coordinates.MidPoint,
                                                                             frameIndexYolo);*/

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
                                            framedItem.Frame = frameYolo;
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

        // TODO(iharwell): Implement a progressively sparse search through the buffer. Consider n-1, n-2, n-4, n-6, n-8, n-12, n-16, n-20, n-24, n-32, n-40, n-48
        public IList<IFramedItem> Run(IFrame frame, Dictionary<string, int> counts, List<(string key, LineSegment coordinates)> lines, ISet<string> category, IList<IFramedItem> items, object sourceObject = null)
        {
            // buffer frame
            int n = _frameBufferLtDNNYolo.Buffer(frame);
            if (n >= 0 && _cached_dnn_outputs.ContainsKey(n))
            {
                _cached_dnn_outputs.Remove(n);
            }
            int frameIndexYolo;
            int frameIndex = frame.FrameIndex;
            if (_counts_prev.Count != 0)
            {
                var trigItems = FindTriggerItems(items);
                if (_frameBufferLtDNNYolo.IsFull && trigItems.Count > 0)
                {
                    // call yolo for crosscheck
                    // _frameDNNYolo.SetTrackingPoint(lines[lineID].coordinates.MidPoint); //only needs to check the last line in each row
                    frameIndexYolo = IndexChooser.FirstIndex(_cached_dnn_outputs, trigItems, frameIndex);
                    //List<YoloTrackingItem> analyzedTrackingItems = null;
                    while (frameIndex - frameIndexYolo + 1 < DNNConfig.FRAME_SEARCH_RANGE && trigItems.Count > 0)
                    {
                        // The buffer mechanism is offset by 1 frame. The last frame in the buffer is the current frame.
                        int bufferIndex = DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo) - 1;
                        //int lineID = Array.IndexOf(counts.Keys.ToArray(), id.TriggerLine);
                        Console.WriteLine("** Calling Cheap on " + bufferIndex + "    Actual Frame: " + frameIndexYolo);
                        IFrame frameYolo = _frameBufferLtDNNYolo[bufferIndex];

                        var analyzedItemCache = GetDnnResults(frameYolo.FrameData, category, frameIndexYolo, sourceObject, Color.Pink);

                        if (analyzedItemCache == null)
                        {
                            frameIndexYolo = IndexChooser.ChooseNextIndex(_cached_dnn_outputs, trigItems, frameIndexYolo, frameIndex);

                            continue;
                        }
                        List<int> nMatches = new List<int>(trigItems.Count);

                        for (int i = 0; i < trigItems.Count; i++)
                        {
                            nMatches.Add(0);
                        }

                        for (int i = 0; i < trigItems.Count && trigItems.Count > 0; i++)
                        {
                            int lineID = Array.IndexOf(counts.Keys.ToArray(), trigItems[i].trigId.TriggerLine);
                            //_frameDNNYolo.SetTrackingPoint(lines[lineID].coordinates.MidPoint);
                            /*IList<IItemID> analyzedTrackingItems;*/
                            var itemOverlaps = LineOverlapFilter.GetItemOverlap(analyzedItemCache, (IItemID id) => new RectangleF(id.BoundingBox.X, id.BoundingBox.Y, id.BoundingBox.Width, id.BoundingBox.Height), _lines[lineID].coordinates);

                            foreach (KeyValuePair<IItemID, float> entry in itemOverlaps)
                            {
                                if (trigItems.Count == 0)
                                {
                                    break;
                                }
                                var yoloTrackingItem = entry.Key;
                                Rectangle bounds = entry.Key.BoundingBox;
                                if (entry.Value >= DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE)
                                {
                                    LineTriggeredItemID itemID = new LineTriggeredItemID(yoloTrackingItem)
                                    {
                                        TriggerLine = lines[lineID].key,
                                        TriggerLineID = lineID,
                                        FurtherAnalysisTriggered = true
                                    };

                                    if (itemID.InsertIntoFramedItemList(items, out IFramedItem framedItem, frameIndexYolo))
                                    {
                                        framedItem.Frame = frameYolo;
                                    }
                                    // output cheap YOLO results
                                    string blobName_Cheap = $@"frame-{frameIndexYolo}-Cheap-{yoloTrackingItem.Confidence}.jpg";

                                    Utils.Utils.SaveFoundItemImage(saveFolders, blobName_Cheap, framedItem, framedItem.ItemIDs.Count - 1, Color.Pink);

                                    /*var outImage = framedItem.TaggedImageData(framedItem.ItemIDs.Count - 1, Color.Pink);

                                    FileStream fstream = new FileStream(fileName_Cheap, FileMode.OpenOrCreate, FileAccess.Write);
                                    outImage.WriteToStream(fstream, ".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
                                    fstream.Flush();
                                    fstream.Close();
                                    fstream.Dispose();
                                    fstream = new FileStream(@OutputFolder.OutputFolderAll + blobName_Cheap, FileMode.OpenOrCreate, FileAccess.Write);
                                    outImage.WriteToStream(fstream, ".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
                                    fstream.Flush();
                                    fstream.Close();
                                    fstream.Dispose();

                                    outImage.Dispose();*/
                                    if(entry.Value < 0.5)
                                    {
                                        ++nMatches[i];
                                        if(nMatches[i]<2)
                                        {
                                            continue;
                                        }
                                    }
                                    UpdateCount(counts);

                                    trigItems.RemoveAt(i);
                                    nMatches.RemoveAt(i);
                                    if (i > 0)
                                    {
                                        i--;
                                    }
                                    break;
                                }
                                else
                                {
                                    ItemID itemID = new ItemID(yoloTrackingItem)
                                    {
                                        IdentificationMethod = nameof(LineTriggeredDNNDarknet),
                                        FurtherAnalysisTriggered = false
                                    };
                                    if (itemID.InsertIntoFramedItemList(items, out var framedItem, frameIndexYolo))
                                    {
                                        framedItem.Frame = frameYolo;
                                    }
                                }

                            }
                        }
                        // object detected by cheap YOLO
                        /*if (analyzedTrackingItems != null)
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
                        }*/
                        frameIndexYolo = IndexChooser.ChooseNextIndex(_cached_dnn_outputs, trigItems, frameIndexYolo, frameIndex);
                        /*
                        frameIndexYolo -= searchStep;
                        while (DNNConfig.FRAME_SEARCH_RANGE - bufferIndex - searchStep > (1 << (searchStep + 1)))
                        {
                            searchStep <<= 1;
                        }*/

                        continue;
                    }
                    AppendCachedResults(items, sourceObject);
                }
            }
            UpdateCount(counts);

            return items;
        }

        private void AppendCachedResults(IList<IFramedItem> items, object sourceObject)
        {
            IFramedItem lastItem = items.Last();
            items.RemoveAt(items.Count - 1);
            var lastItemFrame = lastItem.Frame.FrameIndex;

            var cachedSets = from item in _cached_dnn_outputs
                             where item.Key < lastItemFrame
                             select item;

            foreach (var set in cachedSets)
            {
                int frameNum = set.Key;
                IFrame frameYolo = _frameBufferLtDNNYolo.GetByFrameNumber(frameNum);
                foreach (var yoloTrackingItem in set.Value)
                {
                    Rectangle bounds = yoloTrackingItem.BoundingBox;
                    ItemID itemID = new ItemID(yoloTrackingItem)
                    {
                        IdentificationMethod = nameof(LineTriggeredDNNDarknet),
                        FurtherAnalysisTriggered = false
                    };
                    if (itemID.InsertIntoFramedItemList(items, out var framedItem, frameNum))
                    {
                        framedItem.Frame = frameYolo;
                    }
                }
            }
            items.Add(lastItem);
        }

        private IEnumerable<IItemID> GetDnnResults(Mat frameYolo, ISet<string> category, int frameNumber, object sourceObject, Color color)
        {
            IEnumerable<IItemID> results;
            if (_cached_dnn_outputs.ContainsKey(frameNumber))
            {
                return _cached_dnn_outputs[frameNumber];
            }
            else
            {
                results = _frameDNNYolo.Analyze(frameYolo, category, sourceObject);
                if (results != null)
                {
                    _cached_dnn_outputs.Add(frameNumber, results);
                }
                else
                {
                    _cached_dnn_outputs.Add(frameNumber, new List<IItemID>());
                }
            }

            if (DisplayFrame)
            {
                Mat frame = frameYolo.Clone();
                if (results != null)
                {
                    foreach (var item in results)
                    {
                        frame.Rectangle(new Rect(item.BoundingBox.X, item.BoundingBox.Y, item.BoundingBox.Width, item.BoundingBox.Height), new Scalar(color.B, color.G, color.R));
                    }
                }
                foreach(var item in _lines)
                {
                    frame.Line(new OpenCvSharp.Point(item.coordinates.P1.X, item.coordinates.P1.Y), new OpenCvSharp.Point(item.coordinates.P2.X, item.coordinates.P2.Y), new Scalar(255, 255, 255), 3);
                }
                Cv2.ImShow("LightResults", frame);
                Cv2.WaitKey(1);
                frame.Dispose();
            }

            return results;
        }


        private static IList<(IFramedItem framedItem, ILineTriggeredItemID trigId)> FindTriggerItems(IList<IFramedItem> items)
        {
            IList<(IFramedItem framedItem, ILineTriggeredItemID trigId)> output = new List<(IFramedItem framedItem, ILineTriggeredItemID trigId)>();
            foreach (IFramedItem it in items)
            {
                for (int i = it.ItemIDs.Count - 1; i >= 0; --i)
                {
                    if (it.ItemIDs[i] is ILineTriggeredItemID trig && trig.FurtherAnalysisTriggered)
                    {
                        output.Add((it, trig));
                    }
                }
            }
            return output;
        }

        /*public IList<IFramedItem> Run(Mat frame, int frameIndex, Dictionary<string, int> counts, List<(string key, LineSegment coordinates)> lines, ISet<string> category, IList<IFramedItem> items, object sourceObject = null)
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
                            //_frameDNNYolo.SetTrackingPoint(lines[lineID].coordinates.MidPoint); //only needs to check the last line in each row
                            //Mat[] frameBufferArray = _frameBufferLtDNNYolo.ToArray();
                            int frameIndexYolo = frameIndex - 1;
                            DateTime start = DateTime.Now;
                            List<YoloTrackingItem> analyzedTrackingItems = null;

                            while (frameIndex - frameIndexYolo < DNNConfig.FRAME_SEARCH_RANGE)
                            {
                                Console.WriteLine("** Calling Cheap on " + (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)));
                                Mat frameYolo = _frameBufferLtDNNYolo[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexYolo)];
                                // byte[] imgByte = Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameYolo));
                                var tmp = GetDnnResults(frame, category, Color.Pink, frameIndex);
                                analyzedTrackingItems = _frameDNNYolo.CachedDetect(frameYolo, tmp, lineID, System.Drawing.Color.Pink, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE);
                                analyzedTrackingItems = _frameDNNYolo.Detect(frameYolo,
                                                                             category,
                                                                             lineID,
                                                                             System.Drawing.Color.Pink,
                                                                             DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE,
                                                                             lines[lineID].coordinates.MidPoint,
                                                                             frameIndexYolo);

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
        }*/

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

        private void UpdateCount(Dictionary<string, int> counts)
        {
            foreach (string dir in counts.Keys)
            {
                _counts_prev[dir] = counts[dir];
            }
        }
    }
}
