// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;
using Utils.ShapeTools;

namespace MotionTracker
{
    [DataContract]
    public class CollisionFilter : Filter
    {

        [DataMember]
        private SegmentedPathBuilder _pathBuilder;

        private ISet<IFramedItem> filteredItems;

        public CollisionFilter()
                            : this(8, 8, new IoUPredictor(), 0.6f, 0.4f)
        { }
        public CollisionFilter(int segmentLength, int blockSize, IPathPredictor predictor, float collisionThreshold, float iouThreshold)
        {
            _pathBuilder = new SegmentedPathBuilder();
            _pathBuilder.OnCollision += CollisionHandler;
            SegmentLength = segmentLength;
            BlockSize = blockSize;
            Predictor = predictor;
            CollisionIoUThreshold = collisionThreshold;
            IoUThreshold = iouThreshold;
        }

        [DataMember]
        public int BlockSize { get; set; }

        [DataMember]
        public float CollisionIoUThreshold { get; set; }

        [DataMember]
        public float IoUThreshold { get; set; }

        [DataMember]
        public IPathPredictor Predictor { get => _pathBuilder.Predictor; set => _pathBuilder.Predictor = value; }

        public int SegmentLength { get => _pathBuilder.SegmentLength; set => _pathBuilder.SegmentLength = value; }

        public override IDictionary<IFramedItem, bool> FilterCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            filteredItems = new HashSet<IFramedItem>();
            Dictionary<IFramedItem, bool> filterTable = new();

            for (int i = 0; i < cache.Count; i++)
            {
                for (int j = 0; j < cache[i].Count; j++)
                {
                    if (!filterTable.ContainsKey(cache[i][j]))
                    {
                        filterTable.Add(cache[i][j], true);
                    }
                }
            }
            _pathBuilder.BuildPaths(out var pathTable, out var paths, cache, IoUThreshold);

            foreach (var item in filteredItems)
            {
                filterTable[item] = false;
            }

            return filterTable;
        }

        private void CollisionHandler(object sender, CollisionEventArgs args)
        {
            var item1 = args.Item1;
            var item2 = args.Item2;
            var path1 = args.PathTable[item1];
            var path2 = args.PathTable[item2];

            List<(int, float)> IoUs = new();
            for (int i = path1.FramedItems.Count - 1; i >= 0; --i)
            {
                int frameNum = path1.FrameIndex(i);

                for (int j = path2.FramedItems.Count - 1; j >= 0; --j)
                {
                    if (path2.FrameIndex(j) == frameNum)
                    {
                        IoUs.Add((frameNum, path1.FramedItems[i].MeanBounds.IntersectionOverUnion(path2.FramedItems[j].MeanBounds)));
                    }
                }
            }

            for (int i = 0; i < IoUs.Count; i++)
            {
                if (IoUs[i].Item2 < CollisionIoUThreshold)
                {
                    filteredItems.Add(item1);
                    filteredItems.Add(item2);
                    args.ContinueMerge = false;
                    args.KeepCurrentPathForItem2 = false;
                    break;
                }
            }
        }
    }
}
