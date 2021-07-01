// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;
using Utils.ShapeTools;

namespace Utils
{
    public class SparseIndexChooser : IIndexChooser
    {
        bool FromTheTop = true;

        private class ItemIDWithFrame
        {
            public ItemIDWithFrame(IItemID item, int frameIndex)
            {
                Item = item;
                FrameIndex = frameIndex;
            }

            public IItemID Item { get; set; }
            public int FrameIndex { get; set; }

            public int GetBufferIndex(int latestFrame)
            {
                return latestFrame - FrameIndex;
            }
        }

        public SparseIndexChooser()
        {
            Stride = 1;
        }

        public int BufferDepth { get; set; }

        public int Stride { get; set; }

        public float JitterThreshold = 3.0f;

        private bool IsInBuffer(int frameIndex, int latestFrame)
        {
            return latestFrame - frameIndex + 1 < BufferDepth;
        }

        public int ChooseNextIndex(IDictionary<int, IEnumerable<IItemID>> priorIDs, IList<(IFramedItem, ILineTriggeredItemID)> triggerIDs, int prevIndex, int latestFrame)
        {
            // Rebuild the cache so we can add and remove items, and otherwise work freely without disturbing the original.

            int cacheCount = priorIDs.Count;

            if (cacheCount <= 5)
            {
                if (!priorIDs.ContainsKey(latestFrame + 2 - BufferDepth))
                {
                    return latestFrame + 2 - BufferDepth;
                }
                if (!priorIDs.ContainsKey(latestFrame - BufferDepth / 2))
                {
                    return latestFrame - BufferDepth / 2;
                }
                if (!priorIDs.ContainsKey(latestFrame - BufferDepth / 4))
                {
                    return latestFrame - BufferDepth / 4;
                }
                if (!priorIDs.ContainsKey(latestFrame - BufferDepth / 8))
                {
                    return latestFrame - BufferDepth / 8;
                }
            }
            // Tracked Items - maps ItemIDs to the path that the ID is part of.
            IList<IList<ItemIDWithFrame>> paths = new List<IList<ItemIDWithFrame>>();
            IDictionary<IItemID, IList<ItemIDWithFrame>> trackedItems = new Dictionary<IItemID, IList<ItemIDWithFrame>>();

            // Rebuild the cache so we can add and remove items, and otherwise work freely without disturbing the original.
            IList<IList<IItemID>> rebuiltCache = RebuildCache(priorIDs, latestFrame);

            BuildPaths(paths, trackedItems, rebuiltCache, latestFrame);
            var movingPaths = FilterStationaryItems(rebuiltCache, paths, trackedItems, latestFrame, cacheCount);

            if(movingPaths == null || movingPaths.Count == 0)
            {
                if(cacheCount >= 5)
                {
                    // We have 5 cached frames, and still haven't found a moving target.
                    // This is a false positive.
                    return latestFrame - BufferDepth * 2;
                }
            }


            /*for (int i = 0; i < triggerIDs.Count; i++)
            {
                IList<IList<ItemIDWithFrame>> crossingPaths = FindCrossingPaths(triggerIDs[i], movingPaths);

            }*/
            if (!priorIDs.ContainsKey(latestFrame - (BufferDepth*5) / 8))
            {
                return latestFrame - (BufferDepth * 5) / 8;
            }
            if (!priorIDs.ContainsKey(latestFrame - (BufferDepth * 3) / 8))
            {
                return latestFrame - (BufferDepth * 3) / 8;
            }
            if (FromTheTop)
            {
                FromTheTop = false;
                return latestFrame - Stride;
            }
            return prevIndex - Stride;
        }

        private float DistanceSquared(PointF p1, Point p2)
        {
            float dx = p1.X - p2.X;
            float dy = p1.Y - p2.Y;
            return dx * dx + dy * dy;
        }

        private float DistSq(Point point, Point lineEnd1, Point lineEnd2)
        {
            if (lineEnd1.X == lineEnd2.X)
            {
                // Horizontal line

                Point minYPoint;
                Point maxYPoint;

                if (lineEnd1.Y > lineEnd2.Y)
                {
                    minYPoint = lineEnd2;
                    maxYPoint = lineEnd1;
                }
                else
                {
                    minYPoint = lineEnd1;
                    maxYPoint = lineEnd2;
                }

                int dx = point.X - minYPoint.X;

                if (point.Y < minYPoint.Y)
                {
                    int dy = point.Y - minYPoint.Y;
                    return (dy * dy) + (dx * dx);
                }
                if (point.Y > maxYPoint.Y)
                {
                    int dy = point.Y - maxYPoint.Y;
                    return (dy * dy) + (dx * dx);
                }
                return dx * dx;
            }
            else if (lineEnd1.Y == lineEnd2.Y)
            {
                // Vertical line

                Point minXPoint;
                Point maxXPoint;

                if (lineEnd1.X > lineEnd2.X)
                {
                    minXPoint = lineEnd2;
                    maxXPoint = lineEnd1;
                }
                else
                {
                    minXPoint = lineEnd1;
                    maxXPoint = lineEnd2;
                }
                int dy = point.Y - minXPoint.Y;

                if (point.X < minXPoint.X)
                {
                    int dx = point.X - minXPoint.X;
                    return (dy * dy) + (dx * dx);
                }
                if (point.X > maxXPoint.X)
                {
                    int dx = point.X - maxXPoint.X;
                    return (dy * dy) + (dx * dx);
                }
                return dy * dy;
            }

            Point delta = new Point(lineEnd1.X - lineEnd2.X, lineEnd1.Y - lineEnd2.Y);
            Point perp = new Point(-delta.Y, delta.X);

            float perpSlope = perp.Y / (float)(perp.X);

            float perpPointOffset = point.Y - point.X * perpSlope;
            float perpS1Offset = lineEnd1.Y - lineEnd1.X * perpSlope;
            float perpS2Offset = lineEnd2.Y - lineEnd2.X * perpSlope;

            Point minPerpOffsetPt;
            Point maxPerpOffsetPt;

            if (perpS1Offset > perpS2Offset)
            {
                minPerpOffsetPt = lineEnd2;
                maxPerpOffsetPt = lineEnd1;
            }
            else
            {
                minPerpOffsetPt = lineEnd1;
                maxPerpOffsetPt = lineEnd2;
            }

            if (perpPointOffset < Math.Min(perpS1Offset, perpS2Offset))
            {
                int dx = point.X - minPerpOffsetPt.X;
                int dy = point.Y - minPerpOffsetPt.Y;
                return (dx * dx) + (dy * dy);
            }
            else if (perpPointOffset < Math.Max(perpS1Offset, perpS2Offset))
            {
                int dx = point.X - maxPerpOffsetPt.X;
                int dy = point.Y - maxPerpOffsetPt.Y;
                return (dx * dx) + (dy * dy);
            }
            else
            {
                // form: ax + by + c = 0
                int a = -delta.Y;
                int b = delta.X;
                int c = -(a * lineEnd1.X + b * lineEnd1.Y);

                int num = a * point.X + b * point.Y + c;
                num *= num;

                int den = a * a + b * b;
                return num / (float)den;
            }
        }

        public int FirstIndex(IDictionary<int, IEnumerable<IItemID>> priorIDs, IList<(IFramedItem, ILineTriggeredItemID)> triggerIDs, int latestFrame)
        {
            FromTheTop = true;
            for (int i = 0; i < Stride; i++)
            {
                if (priorIDs.ContainsKey(latestFrame - i))
                {
                    return latestFrame - i;
                }
            }
            return latestFrame;
        }

        // Switch to ItemPaths without using frame data?
        private IList<IList<ItemIDWithFrame>> FilterStationaryItems(IList<IList<IItemID>> rebuiltCache, IList<IList<ItemIDWithFrame>> paths, IDictionary<IItemID, IList<ItemIDWithFrame>> trackedItems, int latestFrame, int cacheCount)
        {
            //IList<IList<IItemID>> rebuiltCache = RebuildCache(rawCache, latestFrame);
            // Tracked Items - maps ItemIDs to the path that the ID is part of.
            //IList<IList<ItemIDWithFrame>> paths = new List<IList<ItemIDWithFrame>>();
            //IDictionary<IItemID, IList<ItemIDWithFrame>> trackedItems = new Dictionary<IItemID, IList<ItemIDWithFrame>>();

            // Check size of cache


            // In order to consider stationary, an object should be present in most frames analyzed, and cannot move significantly in any frame.

            // "Cannot move" implies that the overlap between frames should be very, very high.
            // To be the same object, one might think the DNN ID should be the same in all frames. This may not be true, though.
            // Sometimes, a "car" can be ID'd as a "truck".

            // Note: a stationary object will generally be present in all frames unless obstructed by a moving object.
            IList<IList<ItemIDWithFrame>> filteredPaths = new List<IList<ItemIDWithFrame>>();
            for (int i = 0; i < paths.Count; i++)
            {
                float jitter = CalculateJitter(paths[i]);
                if (jitter < JitterThreshold)
                {
                    RemoveItemsInPathFromSet(paths[i], rebuiltCache, latestFrame);
                }
                else
                {
                    filteredPaths.Add(paths[i]);
                }
            }

            return filteredPaths;
        }
        /*private IList<IList<ItemIDWithFrame>> FilterStationaryItems(IList<IList<IItemID>> rebuiltCache, IList<IList<ItemIDWithFrame>> paths, IDictionary<IItemID, IList<ItemIDWithFrame>> trackedItems, int latestFrame, int cacheCount)
        {
            //IList<IList<IItemID>> rebuiltCache = RebuildCache(rawCache, latestFrame);
            // Tracked Items - maps ItemIDs to the path that the ID is part of.
            //IList<IList<ItemIDWithFrame>> paths = new List<IList<ItemIDWithFrame>>();
            //IDictionary<IItemID, IList<ItemIDWithFrame>> trackedItems = new Dictionary<IItemID, IList<ItemIDWithFrame>>();

            // Check size of cache


            // In order to consider stationary, an object should be present in most frames analyzed, and cannot move significantly in any frame.

            // "Cannot move" implies that the overlap between frames should be very, very high.
            // To be the same object, one might think the DNN ID should be the same in all frames. This may not be true, though.
            // Sometimes, a "car" can be ID'd as a "truck".

            // Note: a stationary object will generally be present in all frames unless obstructed by a moving object.
            IList<IList<ItemIDWithFrame>> filteredPaths = new List<IList<ItemIDWithFrame>>();
            for (int i = 0; i < paths.Count; i++)
            {
                float jitter = CalculateJitter(paths[i]);
                if (jitter < JitterThreshold)
                {
                    RemoveItemsInPathFromSet(paths[i], rebuiltCache, latestFrame);
                }
                else
                {
                    filteredPaths.Add(paths[i]);
                }
            }

            return filteredPaths;
        }*/

        private void RemoveItemsInPathFromSet(IList<ItemIDWithFrame> itemIDWithFrames, IList<IList<IItemID>> rebuiltCache, int latestFrame)
        {
            for (int i = 0; i < itemIDWithFrames.Count; i++)
            {
                var itemToRemove = itemIDWithFrames[i];
                rebuiltCache[itemToRemove.GetBufferIndex(latestFrame)].Remove(itemToRemove.Item);
            }
        }

        private float CalculateJitter(IList<ItemIDWithFrame> itemIDWithFrames)
        {

            if(itemIDWithFrames.Count < 4)
            {
                return 10000;
            }
            // if (itemIDWithFrames.Count < 4)
            {
                float[] frameNum = new float[itemIDWithFrames.Count-1];
                float[] invIous = new float[itemIDWithFrames.Count-1];
                float[] ious = new float[itemIDWithFrames.Count-1];
                Rectangle rect = itemIDWithFrames[0].Item.BoundingBox;
                float minIOU = 2.0f;
                if(itemIDWithFrames.Count == 1)
                {
                    return float.PositiveInfinity;
                }
                for (int i = 1; i < itemIDWithFrames.Count; i++)
                {
                    frameNum[i-1] = itemIDWithFrames[i].FrameIndex;
                    ious[i-1] = rect.IntersectionOverUnion(itemIDWithFrames[i].Item.BoundingBox);
                    if(minIOU > ious[i-1])
                    {
                        minIOU = ious[i-1];
                    }
                    invIous[i-1] = 1.0f/rect.IntersectionOverUnion(itemIDWithFrames[i].Item.BoundingBox);
                }

                float jitter = 0.0f;

                float minFrame = frameNum[0];
                float maxFrame = frameNum[0];

                for (int i = 0; i < itemIDWithFrames.Count-1; i++)
                {
                    jitter += invIous[i];
                    minFrame = Math.Min(frameNum[i], minFrame);
                    maxFrame = Math.Max(frameNum[i], maxFrame);
                }
                jitter /= minIOU * minIOU * (itemIDWithFrames.Count-1);

                return jitter;
            }
        }

        private void BuildPaths(IList<IList<ItemIDWithFrame>> paths, IDictionary<IItemID, IList<ItemIDWithFrame>> trackedItems, IList<IList<IItemID>> rebuiltCache, int latestFrame)
        {

            for (int i = 0; i < BufferDepth; i++)
            {
                // Iterate through the frame cache.
                int currentFrame = latestFrame - i;

                if (rebuiltCache[i] == null)
                {
                    // Skip frames that haven't been analyzed yet
                    // There will be holes in the resulting paths, though.
                    continue;
                }
                for (int j = 0; j < rebuiltCache[i].Count; j++)
                {
                    var itemOfInterest = rebuiltCache[i][j];
                    if (trackedItems.ContainsKey(itemOfInterest))
                    {
                        continue;
                    }
                    BuildPathFromID(itemOfInterest, paths, trackedItems, rebuiltCache, i, latestFrame);
                }
            }
        }

        private void BuildPathFromID(IItemID itemID, IList<IList<ItemIDWithFrame>> paths, IDictionary<IItemID, IList<ItemIDWithFrame>> trackedItems, IList<IList<IItemID>> rebuiltCache, int bufferIndex, int latestFrame)
        {
            var pathList = new List<ItemIDWithFrame>();
            pathList.Add(new ItemIDWithFrame(itemID, latestFrame - bufferIndex));
            float iou;
            IItemID match;
            for (int i = bufferIndex + 1; i < BufferDepth; i++)
            {
                var examinedItemSet = rebuiltCache[i];
                if (examinedItemSet == null)
                {
                    continue;
                }

                (iou, match) = FindBestMatch(itemID, examinedItemSet);
                if (iou > 0.4)
                {
                    pathList.Add(new ItemIDWithFrame(match, latestFrame - i));
                }
            }

            if (pathList.Count < 2)
            {
                return;
            }

            paths.Add(pathList);

            for (int i = 0; i < pathList.Count; i++)
            {
                if( trackedItems.ContainsKey(pathList[i].Item))
                {
                    pathList.RemoveAt(i);
                    i--;
                    continue;
                }
                trackedItems.Add(pathList[i].Item, pathList);
            }
        }

        private IList<IList<IItemID>> RebuildCache(IDictionary<int, IEnumerable<IItemID>> rawCache, int latestFrame)
        {
            IList<IList<IItemID>> rebuiltCache = new List<IList<IItemID>>(BufferDepth);
            for (int i = 0; i < BufferDepth; i++)
            {
                if (rawCache.ContainsKey(latestFrame - i))
                {
                    rebuiltCache.Add(new List<IItemID>(rawCache[latestFrame - i]));
                }
                else
                {
                    rebuiltCache.Add(null);
                }
            }
            return rebuiltCache;
        }

        private static (float iou, IItemID match) FindBestMatch(IItemID testItem, IList<IItemID> searchSet)
        {
            float bestiou = -1.0f;
            IItemID bestMatch = null;

            Rectangle testRect = testItem.BoundingBox;

            for (int i = 0; i < searchSet.Count; i++)
            {
                Rectangle targetRect = searchSet[i].BoundingBox;
                float iou = testRect.IntersectionOverUnion(searchSet[i].BoundingBox);
                if (iou > bestiou)
                {
                    bestiou = iou;
                    bestMatch = searchSet[i];
                }
            }

            return (bestiou, bestMatch);
        }
    }
}
