// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;

namespace MotionTracker
{
    public class CollisionFilter : ICacheFilter
    {

        public CollisionFilter()
            : this(8, 8, new IoUPredictor(), 0.6f, 0.4f)
        { }
        public CollisionFilter(int segmentLength, int blockSize, IPathPredictor predictor, float collisionThreshold, float iouThreshold)
        {
            _pathBuilder = new SegmentedPathBuilder();
            SegmentLength = segmentLength;
            BlockSize = blockSize;
            Predictor = predictor;
            CollisionIoUThreshold = collisionThreshold;
            IoUThreshold = iouThreshold;
        }

        public int SegmentLength { get => _pathBuilder.SegmentLength; set => _pathBuilder.SegmentLength = value; }
        public int BlockSize { get; set; }
        public IPathPredictor Predictor { get; set; }
        public float CollisionIoUThreshold { get; set; }
        public float IoUThreshold { get; set; }

        private SegmentedPathBuilder _pathBuilder;

        public IDictionary<IFramedItem, bool> FilterCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            _pathBuilder.BuildPaths(out var pathTable, out var paths, cache, IoUThreshold);

            throw new NotImplementedException();
        }

        public IList<IList<IFramedItem>> GetFilteredCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            throw new NotImplementedException();
        }
    }
}
