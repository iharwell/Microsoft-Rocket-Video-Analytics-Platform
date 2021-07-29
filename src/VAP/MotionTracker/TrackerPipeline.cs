// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Utils.Items;

namespace MotionTracker
{
    [DataContract]
    [KnownType(typeof(TimeCompressor))]
    [KnownType(typeof(ItemPath))]
    [KnownType(typeof(MotionTracker))]
    public class TrackerPipeline
    {
        private IList<IItemPath> _paths;

        [DataMember]
        public int BufferSize { get; set; }

        [DataMember]
        public float MergeSimilarityThreshold { get; set; }

        [DataMember]
        public MotionTracker Tracker { get; set; }

        [DataMember]
        public TimeCompressor Compressor { get; set; }

        public IList<IItemPath> Paths => _paths;

        public IList<IList<IFramedItem>> ItemBuffer { get; private set; }

        public Func<IFramedItem, bool> ShouldStartPathFunc { get; set; }

        public TrackerPipeline()
        {
            _paths = new List<IItemPath>();
            ItemBuffer = new List<IList<IFramedItem>>();
            MergeSimilarityThreshold = 0.5f;
            BufferSize = 150;
        }

        public void WriteAllPaths()
        {
            while (Paths.Count > 0)
            {
                WritePath(0, Paths[0]);
            }
        }

        public void PostDummyFrame(IFrame frame)
        {
            List<IFramedItem> dummylist = new List<IFramedItem>();
            IFramedItem dummyItem = new FramedItem
            {
                Frame = frame,
            };
            dummyItem.ItemIDs.Add(new FillerID());
            dummylist.Add(dummyItem);
            ItemBuffer = Motion.InsertIntoSortedBuffer(ItemBuffer, dummylist, MergeSimilarityThreshold, 0);
            while (ItemBuffer.Count > BufferSize || (ItemBuffer.Count > 0 && ItemBuffer[0].Count == 0))
            {
                ItemBuffer.RemoveAt(0);
            }
        }

        public void PostFramedItems(IList<IFramedItem> items, int currentFrameIndex)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }
            if ((currentFrameIndex & 0x3F) == 0)
            {
                ExtendExistingPaths();
            }
            bool pathFound = false;
            foreach (var item in items)
            {
                bool startPath = ShouldStartPathFunc?.Invoke(item) ?? true;

                if (startPath)
                {
                    var path = Tracker.BuildPath(item, ItemBuffer, false);
                    if (path != null)
                    {
                        Motion.SealPath(path, ItemBuffer);
                        Paths.Add(path);
                        pathFound = true;
                    }
                }
            }
            if (pathFound && Paths.Count > 1)
            {
                Tracker.TryMergePaths(ref _paths, MergeSimilarityThreshold);
            }

            WritePaths(currentFrameIndex);

            ItemBuffer = Motion.InsertIntoSortedBuffer(ItemBuffer, items, MergeSimilarityThreshold, 0);

            while (ItemBuffer.Count > BufferSize || ItemBuffer[0].Count == 0)
            {
                ItemBuffer.RemoveAt(0);
            }

            ItemBuffer = Motion.GroupByFrame(ItemBuffer, MergeSimilarityThreshold, MergeSimilarityThreshold / 2);
        }

        private void ExtendExistingPaths()
        {
            Tracker.TryMergePaths(ref _paths, MergeSimilarityThreshold);
            if (Paths.Count > 1)
            {
                void func(IItemPath path)
                {
                    int preCount = path.FramedItems.Count;

                    Tracker.ExtendPath(path, ItemBuffer, true);
                    if (path.FramedItems.Count > preCount)
                    {
                        Motion.SealPath(path, ItemBuffer);
                    }

                    if (path.FramedItems.Count > BufferSize * 2)
                    {
                        Compressor.ProcessPath(path);
                    }
                }


                //Parallel.ForEach(itemPaths, func);
                for (int i = 0; i < Paths.Count; i++)
                {
                    func(Paths[i]);
                }
            }
            //for (int i = 0; i < itemPaths.Count; i++)
            else if (Paths.Count > 0)
            {
                var path = Paths[0];
                int preCount = path.FramedItems.Count;

                Tracker.ExtendPath(path, ItemBuffer, true);
                if (path.FramedItems.Count > preCount)
                {
                    Motion.SealPath(path, ItemBuffer);
                }

                if (path.FramedItems.Count > BufferSize * 2)
                {
                    Compressor.ProcessPath(path);
                }
            }
        }

        private void WritePaths(int currentFrame)
        {
            for (int i = 0; i < Paths.Count; i++)
            {
                while (i < Paths.Count && Paths.Count > 0 && Paths[i].GetPathBounds().maxFrame < currentFrame - BufferSize)
                {
                    var path = Paths[i];
                    Compressor?.ProcessPath(path);
                    Tracker.ScrubPath(path);
                    WritePath(i, path);
                }
            }
        }

        private void WritePath(int index, IItemPath path)
        {
            int conFrameIndex = path.HighestConfidenceFrameIndex;
            int conIDIndex = path.HighestConfidenceIDIndex;
            IFrame conframe = path.FramedItems[conFrameIndex].Frame;
            string mp4Name = GetPathFileName(path) + ".mp4";
            string xmlName = GetPathFileName(path) + ".xml";

            double frameRate = path.FramedItems[0].Frame.FrameRate;

            Console.WriteLine($"\tWriting output path to {mp4Name} and {xmlName}");
            // FourCC code = FourCC.X264;
            // FourCC code = FourCC.FromFourChars('x', '2', '6', '4');
            FourCC code = FourCC.FromFourChars('m', 'p', '4', 'v');
            // FourCC code = FourCC.FromFourChars('h', '2', '6', '5');
            OpenCvSharp.VideoWriter writer = new VideoWriter(Utils.Config.OutputFolder.OutputFolderVideo + mp4Name, code, frameRate, conframe.FrameData.Size());

            var fItems = from fi in path.FramedItems
                         let f = fi.Frame
                         orderby f.FrameIndex
                         select fi;

            foreach (IFramedItem frameItem in fItems)
            {
                Mat frameData = frameItem.Frame.FrameData.Clone();
                if (!(frameItem.ItemIDs.Count == 1 && frameItem.ItemIDs[0] is FillerID))
                {
                    var median = frameItem.MeanBounds;

                    Cv2.Rectangle(frameData, new Rect((int)median.X, (int)median.Y, (int)median.Width, (int)median.Height), Scalar.Red);
                }

                writer.Write(frameData);
            }
            writer.Release();
            writer.Dispose();
            DataContractSerializer serializer = new DataContractSerializer(typeof(ItemPath));
            if (path is ItemPath p)
            {
                using StreamWriter swriter = new StreamWriter(Utils.Config.OutputFolder.OutputFolderXML + xmlName + ".xml");
                serializer.WriteObject(swriter.BaseStream, p);
                swriter.Flush();
                swriter.Close();
            }
            {
                var pa = Paths[index];
                for (int i = 0; i < pa.FramedItems.Count; i++)
                {
                    pa.FramedItems[i] = null;
                }
            }

            Paths.RemoveAt(index);
        }

        private string GetPathFileName(IItemPath path)
        {
            IFramedItem earliestID = null;
            IFramedItem latestID = null;
            IFramedItem bestID = null;
            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                var fi = path.FramedItems[i];
                if (earliestID is null || earliestID.Frame.FrameIndex > fi.Frame.FrameIndex)
                {
                    earliestID = fi;
                }
                if (latestID is null || latestID.Frame.FrameIndex < fi.Frame.FrameIndex)
                {
                    latestID = fi;
                }
                if (bestID is null || bestID.ItemIDs[bestID.HighestConfidenceIndex].Confidence < fi.ItemIDs[fi.HighestConfidenceIndex].Confidence)
                {
                    bestID = fi;
                }
            }

            var timeStamp = earliestID.Frame.TimeStamp;
            var timeCode1 = $"{timeStamp.Year}{timeStamp.Month.ToString("D2")}{timeStamp.Day.ToString("D2")}{timeStamp.Hour.ToString("D2")}{timeStamp.Minute.ToString("D2")}{timeStamp.Second.ToString("D2")}";

            timeStamp = latestID.Frame.TimeStamp;
            var timeCode2 = $"{timeStamp.Year}{timeStamp.Month.ToString("D2")}{timeStamp.Day.ToString("D2")}{timeStamp.Hour.ToString("D2")}{timeStamp.Minute.ToString("D2")}{timeStamp.Second.ToString("D2")}";

            return $"{timeCode1}-{timeCode2}-{bestID.ItemIDs[bestID.HighestConfidenceIndex].ObjName}";
        }
    }
}
