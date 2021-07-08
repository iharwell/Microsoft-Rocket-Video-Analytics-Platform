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
using Utils;

namespace MotionTracker
{
    public class SparseIndexChooser : IIndexChooser
    {
        bool FromTheTop = true;
        bool BufferChecked = false;

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
            Stride = 3;
        }

        public int BufferDepth
        {
            get => _bufferDepth;
            set
            {
                _bufferDepth = value;
                IndexOrder = new int[_bufferDepth];
                IndexOrder[0] = _bufferDepth - 2;
                for (int i = 1; i < _bufferDepth; i++)
                {
                    int den = 2;
                    while (i >= den)
                    {
                        den <<= 1;
                    }
                    int num = 1 + 2 * ((i) & ((den >> 1) - 1));
                    int targetIndex = (_bufferDepth * num) / den;
                    if (IndexOrder.Contains(targetIndex))
                    {
                        int tgtPlus = targetIndex;
                        while (IndexOrder.Contains(tgtPlus))
                        {
                            tgtPlus++;
                        }
                        int tgtMinus = targetIndex;
                        while (IndexOrder.Contains(tgtMinus))
                        {
                            tgtMinus--;
                        }

                        if (tgtMinus < 0)
                        {
                            tgtMinus = -3 * BufferDepth;
                        }

                        if (tgtPlus >= BufferDepth)
                        {
                            tgtPlus = 4 * BufferDepth;
                        }

                        if( tgtPlus - targetIndex > targetIndex - tgtMinus)
                        {
                            IndexOrder[i] = tgtMinus;
                        }
                        else
                        {
                            IndexOrder[i] = tgtPlus;
                        }
                    }
                    else
                    {
                        IndexOrder[i] = targetIndex;
                    }
                }
            }
        }

        public int Stride { get; set; }

        public float JitterThreshold = 7.0f;

        private bool IsInBuffer(int frameIndex, int latestFrame)
        {
            return latestFrame - frameIndex + 1 < BufferDepth;
        }

        int BufferIndex = 0;

        private int[] IndexOrder;
        private int _bufferDepth;

        public int ChooseNextIndex(IDictionary<int, IEnumerable<IItemID>> priorIDs, IList<(IFramedItem, ILineTriggeredItemID)> triggerIDs, int prevIndex, int latestFrame)
        {
            // Rebuild the cache so we can add and remove items, and otherwise work freely without disturbing the original.

            if (!BufferChecked)
            {
                var ids = priorIDs.ToArray();
                if(BufferIndex>=ids.Length)
                {
                    BufferChecked = true;
                }
                else
                {
                    if (ids[BufferIndex].Key == latestFrame)
                    {
                        BufferIndex++;
                    }
                    BufferIndex++;
                    if (BufferIndex > ids.Length)
                    {
                        BufferChecked = true;
                    }
                    else
                    {
                        return ids[BufferIndex - 1].Key;
                    }
                }
            }

            int cacheCount = priorIDs.Count;

            if (cacheCount <= 5)
            {
                int index = 0;
                while (priorIDs.ContainsKey(latestFrame - IndexOrder[index]))
                {
                    index++;
                }
                return latestFrame - IndexOrder[index];
                /*if (!priorIDs.ContainsKey(latestFrame - BufferDepth / 4))
                {
                    return latestFrame - BufferDepth / 4;
                }
                if (!priorIDs.ContainsKey(latestFrame - BufferDepth / 2))
                {
                    return latestFrame - BufferDepth / 2;
                }
                if (!priorIDs.ContainsKey(latestFrame - BufferDepth * 3 / 4))
                {
                    return latestFrame - BufferDepth * 3 / 4;
                }
                if (!priorIDs.ContainsKey(latestFrame + 2 - BufferDepth))
                {
                    return latestFrame + 2 - BufferDepth;
                }*/
            }
            // Tracked Items - maps ItemIDs to the path that the ID is part of.
            IList<IList<ItemIDWithFrame>> paths = new List<IList<ItemIDWithFrame>>();
            IDictionary<IItemID, IList<IList<ItemIDWithFrame>>> trackedItems = new Dictionary<IItemID, IList<IList<ItemIDWithFrame>>>();

            // Rebuild the cache so we can add and remove items, and otherwise work freely without disturbing the original.
            IList<IList<IItemID>> rebuiltCache = RebuildCache(priorIDs, latestFrame);

            BuildPaths(paths, trackedItems, rebuiltCache, latestFrame);
            var movingPaths = FilterStationaryItems(rebuiltCache, paths, trackedItems, latestFrame, cacheCount);

            if (movingPaths == null || movingPaths.Count == 0 || cacheCount >= BufferDepth / Stride)
            {
                if (cacheCount > 5)
                {
                    // We have 5 cached frames, and still haven't found a moving target.
                    // This is a false positive.
                    return latestFrame - BufferDepth * 2;
                }
            }
            if (cacheCount > 10)
            {
                int longestPath = 0;
                for (int i = 0; i < movingPaths.Count; i++)
                {
                    longestPath = Math.Max(movingPaths[i].Count, longestPath);
                }
                if (longestPath * 5 < cacheCount)
                {
                    return latestFrame - BufferDepth * 2;
                }
            }

            int bestCrossingIndex = -1;
            int bestConfidence = 0;
            /*
            for (int i = 0; i < triggerIDs.Count; i++)
            {
                (int index, int confidence) = FindBestCrossingIndex(triggerIDs[i], movingPaths, cacheCount);

                if (confidence > bestConfidence)
                {
                    bestConfidence = confidence;
                    bestCrossingIndex = index;
                }
            }

            if (bestCrossingIndex > 0 && bestCrossingIndex <= latestFrame)
            {
                return ClosestUntestedFrame(bestCrossingIndex, latestFrame, priorIDs);
            }*/

            {
                int index = 0;
                while (priorIDs.ContainsKey(latestFrame - IndexOrder[index]))
                {
                    index++;
                }
                return latestFrame - IndexOrder[index];
            }
            /*
            if (!priorIDs.ContainsKey(latestFrame - BufferDepth / 8))
            {
                return latestFrame - BufferDepth / 8;
            }
            if (!priorIDs.ContainsKey(latestFrame - (BufferDepth * 3) / 8))
            {
                return latestFrame - (BufferDepth * 3) / 8;
            }
            if (!priorIDs.ContainsKey(latestFrame - (BufferDepth * 5) / 8))
            {
                return latestFrame - (BufferDepth * 5) / 8;
            }
            if (!priorIDs.ContainsKey(latestFrame - (BufferDepth * 7) / 8))
            {
                return latestFrame - (BufferDepth * 5) / 8;
            }
            int proposedFrame = latestFrame - Stride;
            while (priorIDs.ContainsKey(proposedFrame))
            {
                proposedFrame -= Stride;
            }
            return proposedFrame;*/
        }

        private int ClosestUntestedFrame(int frameNumber, int latestFrame, IDictionary<int, IEnumerable<IItemID>> priorIDs)
        {
            for (int i = 0; i < BufferDepth; i++)
            {
                int frameHigh = frameNumber + i;
                if (frameHigh <= latestFrame && !priorIDs.ContainsKey(frameHigh))
                {
                    return frameHigh;
                }
                int frameLow = frameNumber - i;
                if (frameLow >= latestFrame - BufferDepth && !priorIDs.ContainsKey(frameLow))
                {
                    return frameLow;
                }
                if( frameHigh > latestFrame && frameLow < latestFrame-BufferDepth)
                {
                    break;
                }
            }
            return -1;
        }

        private (int index, int confidence) FindBestCrossingIndex((IFramedItem, ILineTriggeredItemID) p, IList<IList<ItemIDWithFrame>> movingPaths, int cacheCount)
        {
            LineSegment triggerLine = p.Item2.TriggerSegment;

            IList<IList<ItemIDWithFrame>> crossingPaths = new List<IList<ItemIDWithFrame>>();

            int pathIndex = -1;
            int index = -1;
            int bestconfidence = 0;
            int frameNumber = -1;

            for (int i = 0; i < movingPaths.Count; i++)
            {
                List<ItemIDWithFrame> path = new List<ItemIDWithFrame>(movingPaths[i]);
                path.Sort((ItemIDWithFrame item1, ItemIDWithFrame item2) => item1.FrameIndex - item2.FrameIndex);
                List<(LRPosition, TBPosition, Point)> positions = new();
                for (int j = 0; j < path.Count; j++)
                {
                    Rectangle rect = path[j].Item.BoundingBox;
                    (var lr, var tb) = rect.DeterminePositionSituation(triggerLine);
                    Point closest = rect.GetNearestPointToLine(triggerLine);
                    positions.Add((lr, tb, closest));
                }

                if (cacheCount > 10 && IsOneSided(positions, triggerLine, out var lrIndicator, out var tbIndicator))
                {
                    if (lrIndicator == LRPosition.SegmentLeftPart || lrIndicator == LRPosition.SegmentRightPart
                        || tbIndicator == TBPosition.SegmentAbovePart || tbIndicator == TBPosition.SegmentBelowPart)
                    {
                        for (int j = 0; j < path.Count; j++)
                        {

                        }
                    }
                    else
                    {
                        int closestIndex = 0;
                        float closestdist = float.MaxValue;
                        ItemIDWithFrame closestItemID = null;
                        for (int j = 0; j < path.Count; j++)
                        {
                            float distsq = DistSq(positions[j].Item3, triggerLine.P1, triggerLine.P2);
                            if(distsq < closestdist)
                            {
                                closestdist = distsq;
                                closestIndex = j;
                                closestItemID = path[j];
                            }
                        }
                        return (closestItemID.FrameIndex, 11);
                    }
                }

                (int lowerCrossIndex, int confidence) = DetermineLowerCrossingIndex(positions, triggerLine);
                if (confidence > bestconfidence)
                {
                    index = lowerCrossIndex;
                    bestconfidence = confidence;
                    pathIndex = i;

                    ItemIDWithFrame p1 = path[index];
                    ItemIDWithFrame p2 = path[index + 1];

                    float d1 = DistSq(positions[index].Item3, triggerLine.P1, triggerLine.P2);
                    float d2 = DistSq(positions[index + 1].Item3, triggerLine.P1, triggerLine.P2);
                    float ratio = d1 / (d1 + d2);

                    frameNumber = p1.FrameIndex + (int)((p2.FrameIndex - p1.FrameIndex) * ratio + 0.5);
                }
            }

            if (index == -1)
            {
                return (-1, 0);
            }

            return (frameNumber, bestconfidence);

            //throw new NotImplementedException();
        }

        private (int index, int confidence) DetermineLowerCrossingIndex(List<(LRPosition lr, TBPosition tb, Point closest)> positions, LineSegment segment)
        {
            var prevLR = positions[0].lr;
            var prevTB = positions[0].tb;
            var prevClosest = positions[0].closest;

            for (int i = 1; i < positions.Count; i++)
            {
                (var lr, var tb, var closest) = positions[i];
                if (HasCrossedThrough(lr, tb, prevLR, prevTB))
                {
                    return (i - 1, 10);
                }
                prevLR = lr;
                prevTB = tb;
                prevClosest = closest;
            }

            prevLR = positions[0].lr;
            prevTB = positions[0].tb;
            prevClosest = positions[0].closest;
            for (int i = 1; i < positions.Count; i++)
            {
                (var lr, var tb, var closest) = positions[i];
                if (prevTB != tb || lr != prevLR)
                {
                    return (i - 1, 8);
                }
            }

            float shortestDist = DistSq(positions[0].closest, segment.P1, segment.P2);
            int shortestIndex = 0;
            for (int i = 1; i < positions.Count; i++)
            {
                float distSq = DistSq(positions[0].closest, segment.P1, segment.P2);
                if (distSq < shortestDist)
                {
                    shortestIndex = i;
                    shortestDist = distSq;
                }
            }
            if (shortestIndex > 0 && shortestDist < (positions.Count - 1))
            {
                float prevDist = DistSq(positions[shortestIndex - 1].closest, segment.P1, segment.P2);
                float nextDist = DistSq(positions[shortestIndex + 1].closest, segment.P1, segment.P2);
                if (prevDist < nextDist)
                {
                    return (shortestIndex - 1, 7);
                }
                return (shortestIndex, 7);
            }
            if (shortestIndex == positions.Count - 1)
            {
                return (shortestIndex - 1, 6);
            }
            return (shortestIndex, 6);
        }

        private bool IsOneSided(List<(LRPosition lr, TBPosition tb, Point closest)> positions, LineSegment segment, out LRPosition lrPosition, out TBPosition tbPosition)
        {
            Dictionary<LRPosition, int> lrCounts = new()
            {
                { LRPosition.SegmentLeftFull, 0 },
                { LRPosition.SegmentLeftPart, 0 },
                { LRPosition.SegmentInner, 0 },
                { LRPosition.SegmentStraddle, 0 },
                { LRPosition.SegmentRightPart, 0 },
                { LRPosition.SegmentRightFull, 0 },
            };
            Dictionary<TBPosition, int> tbCounts = new()
            {
                { TBPosition.SegmentAboveFull, 0 },
                { TBPosition.SegmentAbovePart, 0 },
                { TBPosition.SegmentInner, 0 },
                { TBPosition.SegmentStraddle, 0 },
                { TBPosition.SegmentBelowPart, 0 },
                { TBPosition.SegmentBelowFull, 0 },
            };

            var prevLR = positions[0].lr;
            var prevTB = positions[0].tb;
            var prevClosest = positions[0].closest;

            for (int i = 1; i < positions.Count; i++)
            {
                (var lr, var tb, var closest) = positions[i];
                lrCounts[lr] = lrCounts[lr] + 1;
                tbCounts[tb] = tbCounts[tb] + 1;
            }

            int[] lrCountArr = new int[6];
            int[] tbCountArr = new int[6];

            lrCountArr[0] = lrCounts[LRPosition.SegmentLeftFull];
            lrCountArr[1] = lrCounts[LRPosition.SegmentLeftPart];
            lrCountArr[2] = lrCounts[LRPosition.SegmentInner];
            lrCountArr[3] = lrCounts[LRPosition.SegmentStraddle];
            lrCountArr[4] = lrCounts[LRPosition.SegmentRightPart];
            lrCountArr[5] = lrCounts[LRPosition.SegmentRightFull];

            tbCountArr[0] = tbCounts[TBPosition.SegmentAboveFull];
            tbCountArr[1] = tbCounts[TBPosition.SegmentAbovePart];
            tbCountArr[2] = tbCounts[TBPosition.SegmentInner];
            tbCountArr[3] = tbCounts[TBPosition.SegmentStraddle];
            tbCountArr[4] = tbCounts[TBPosition.SegmentBelowPart];
            tbCountArr[5] = tbCounts[TBPosition.SegmentBelowFull];

            if (lrCountArr[0] == positions.Count)
            {
                tbPosition = TBPosition.SegmentStraddle;
                lrPosition = LRPosition.SegmentLeftFull;
                return true;
            }
            if (lrCountArr[5] == positions.Count)
            {
                tbPosition = TBPosition.SegmentStraddle;
                lrPosition = LRPosition.SegmentRightFull;
                return true;
            }
            if (tbCountArr[0] == positions.Count)
            {
                tbPosition = TBPosition.SegmentAboveFull;
                lrPosition = LRPosition.SegmentStraddle;
                return true;
            }
            if (tbCountArr[5] == positions.Count)
            {
                tbPosition = TBPosition.SegmentBelowFull;
                lrPosition = LRPosition.SegmentStraddle;
                return true;
            }

            if (lrCountArr[0] + lrCountArr[1] == positions.Count)
            {
                tbPosition = TBPosition.SegmentStraddle;
                lrPosition = LRPosition.SegmentLeftPart;
                return true;
            }
            if (lrCountArr[5] + lrCountArr[4] == positions.Count)
            {
                tbPosition = TBPosition.SegmentStraddle;
                lrPosition = LRPosition.SegmentLeftPart;
                return true;
            }
            if (tbCountArr[0] + tbCountArr[1] == positions.Count)
            {
                tbPosition = TBPosition.SegmentAbovePart;
                lrPosition = LRPosition.SegmentStraddle;
                return true;
            }
            if (tbCountArr[5] + tbCountArr[4] == positions.Count)
            {
                tbPosition = TBPosition.SegmentBelowPart;
                lrPosition = LRPosition.SegmentStraddle;
                return true;
            }
            tbPosition = TBPosition.SegmentStraddle;
            lrPosition = LRPosition.SegmentStraddle;
            return false;
        }

        private bool HasCrossedThrough(LRPosition lr, TBPosition tb, LRPosition prevLR, TBPosition prevTB)
        {
            sbyte slr = (sbyte)lr;
            sbyte stb = (sbyte)tb;
            sbyte splr = (sbyte)prevLR;
            sbyte sptb = (sbyte)prevTB;

            if (lr == prevLR && (lr == LRPosition.SegmentLeftFull || lr == LRPosition.SegmentRightFull))
            {
                return false;
            }

            if (tb == prevTB && (tb == TBPosition.SegmentAboveFull || tb == TBPosition.SegmentBelowFull))
            {
                return false;
            }

            if (splr * slr < 0)
            {
                if (prevTB != tb)
                {
                    return true;
                }
                else if (prevTB != TBPosition.SegmentAboveFull && prevTB != TBPosition.SegmentBelowFull)
                {
                    return true;
                }
                else if (tb != TBPosition.SegmentAboveFull && tb != TBPosition.SegmentBelowFull)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            else if (stb * sptb < 0)
            {
                if (prevLR != lr)
                {
                    return true;
                }
                else if (prevLR != LRPosition.SegmentLeftFull && prevLR != LRPosition.SegmentRightFull)
                {
                    return true;
                }
                else if (lr != LRPosition.SegmentLeftFull && lr != LRPosition.SegmentRightFull)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private static float DistanceSquared(PointF p1, Point p2)
        {
            float dx = p1.X - p2.X;
            float dy = p1.Y - p2.Y;
            return dx * dx + dy * dy;
        }

        private static float DistSq(Point point, Point lineEnd1, Point lineEnd2)
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
            BufferChecked = false;

            BufferIndex = 0;
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
        private IList<IList<ItemIDWithFrame>> FilterStationaryItems(IList<IList<IItemID>> rebuiltCache,
                                                                    IList<IList<ItemIDWithFrame>> paths,
                                                                    IDictionary<IItemID, IList<IList<ItemIDWithFrame>>> trackedItems,
                                                                    int latestFrame,
                                                                    int cacheCount)
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
                    RemoveItemsInPathFromSet(paths[i], rebuiltCache, latestFrame, trackedItems);
                }
                else
                {
                    filteredPaths.Add(paths[i]);
                }
            }

            return filteredPaths;
        }

        private static void RemoveItemsInPathFromSet(IList<ItemIDWithFrame> itemIDWithFrames,
                                                     IList<IList<IItemID>> rebuiltCache,
                                                     int latestFrame,
                                                     IDictionary<IItemID, IList<IList<ItemIDWithFrame>>> trackedItems)
        {
            for (int i = 0; i < itemIDWithFrames.Count; i++)
            {
                var itemToRemove = itemIDWithFrames[i];
                if (trackedItems[itemToRemove.Item].Count > 1)
                {
                    continue;
                }
                rebuiltCache[itemToRemove.GetBufferIndex(latestFrame)].Remove(itemToRemove.Item);
            }
        }

        private static float CalculateJitter(IList<ItemIDWithFrame> itemIDWithFrames)
        {

            if (itemIDWithFrames.Count < 4)
            {
                return 10000;
            }
            // if (itemIDWithFrames.Count < 4)
            {
                float[] frameNum = new float[itemIDWithFrames.Count - 1];
                float[] invIous = new float[itemIDWithFrames.Count - 1];
                float[] ious = new float[itemIDWithFrames.Count - 1];
                Rectangle rect = itemIDWithFrames[0].Item.BoundingBox;
                float minIOU = 2.0f;
                if (itemIDWithFrames.Count == 1)
                {
                    return float.PositiveInfinity;
                }
                for (int i = 1; i < itemIDWithFrames.Count; i++)
                {
                    frameNum[i - 1] = itemIDWithFrames[i].FrameIndex;
                    ious[i - 1] = rect.IntersectionOverUnion(itemIDWithFrames[i].Item.BoundingBox);
                    if (minIOU > ious[i - 1])
                    {
                        minIOU = ious[i - 1];
                    }
                    invIous[i - 1] = 1.0f / rect.IntersectionOverUnion(itemIDWithFrames[i].Item.BoundingBox);
                }

                float jitter = 0.0f;

                float minFrame = frameNum[0];
                float maxFrame = frameNum[0];

                for (int i = 0; i < itemIDWithFrames.Count - 1; i++)
                {
                    jitter += invIous[i];
                    minFrame = Math.Min(frameNum[i], minFrame);
                    maxFrame = Math.Max(frameNum[i], maxFrame);
                }
                jitter /= minIOU * minIOU * (itemIDWithFrames.Count - 1);

                return jitter;
            }
        }

        private void BuildPaths(IList<IList<ItemIDWithFrame>> paths, IDictionary<IItemID, IList<IList<ItemIDWithFrame>>> trackedItems, IList<IList<IItemID>> rebuiltCache, int latestFrame)
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

        private void BuildPathFromID(IItemID itemID, IList<IList<ItemIDWithFrame>> paths, IDictionary<IItemID, IList<IList<ItemIDWithFrame>>> trackedItems, IList<IList<IItemID>> rebuiltCache, int bufferIndex, int latestFrame)
        {
            var pathList = new List<ItemIDWithFrame>
            {
                new ItemIDWithFrame(itemID, latestFrame - bufferIndex)
            };
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
                if (trackedItems.ContainsKey(pathList[i].Item))
                {
                    trackedItems[pathList[i].Item].Add(pathList);
                    continue;
                }
                else
                {
                    var l = new List<IList<ItemIDWithFrame>>() { pathList };
                    trackedItems.Add(pathList[i].Item, l);

                }
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
