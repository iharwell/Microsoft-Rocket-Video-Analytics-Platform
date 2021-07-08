// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Utils.Items;
using Utils.ShapeTools;

namespace MotionTracker
{
    /// <summary>
    /// Provides motion tracking support for <see cref="IFramedItem"/> sets.
    /// </summary>
    public class MotionTracker
    {
        public MotionTracker()
        {
            Filters = new List<ICacheFilter>();
            Predictors = new List<(IPathPredictor module, float threshold)>();
            ThresholdCoefProgression = new List<float>()
            {
                0.5f,
                0.3f,
                0.1f
            };
            LatestFrameAnalyzed = new();
            LatestAnalyzer = new();
        }

        public IList<ICacheFilter> Filters { get; protected set; }
        public IList<(IPathPredictor module, float threshold)> Predictors { get; protected set; }
        private Dictionary<IItemPath, IPathPredictor> LatestAnalyzer { get; set; }
        private Dictionary<IItemPath, int> LatestFrameAnalyzed { get; set; }

        public IList<float> ThresholdCoefProgression { get; set; }
        /// <summary>
        ///   Sorts a buffer of items with arbitruary order by frame number.
        /// </summary>
        /// <param name="buffer">
        ///   The buffer to sort.
        /// </param>
        /// <returns>
        ///   Returns a sorted buffer where the first index corresponds to a specific frame number common to all items at that index.
        /// </returns>
        public static IList<IList<IFramedItem>> GroupByFrame(IList<IList<IFramedItem>> buffer, double mergeThreshold, double nameBoost)
        {
            if (buffer.Count == 1)
            {
                int frame = -1;
                if (buffer[0].Count > 0)
                {
                    frame = buffer[0][0].Frame.FrameIndex;
                }
                bool sameFrame = true;
                for (int i = 1; i < buffer[0].Count; i++)
                {
                    var item = buffer[0][i];
                    if (item.Frame.FrameIndex != frame)
                    {
                        sameFrame = false;
                        break;
                    }
                }
                if (sameFrame)
                {
                    return buffer;
                }
            }

            bool alreadySorted = true;
            for (int i = buffer.Count - 1; i >= 0; --i)
            {
                if (!AreAllSameFrame(buffer[i]))
                {
                    alreadySorted = false;
                    break;
                }
            }
            if (alreadySorted)
            {
                return buffer;
            }

            var allFramedItems = from IList<IFramedItem> subList in buffer
                                 from IFramedItem item in subList
                                 group item by item.Frame.FrameIndex;
            //select item;

            int minFrame = buffer[0][0].Frame.FrameIndex;
            int maxFrame = buffer[0][0].Frame.FrameIndex;

            foreach (var grouping in allFramedItems)
            {
                int frameIndex = grouping.Key;
                minFrame = Math.Min(minFrame, frameIndex);
                maxFrame = Math.Max(maxFrame, frameIndex);
            }

            IList<IList<IFramedItem>> organizedFrames = new List<IList<IFramedItem>>(maxFrame - minFrame + 1);

            for (int i = minFrame; i <= maxFrame; i++)
            {
                organizedFrames.Add(new List<IFramedItem>());
            }

            foreach (var grouping in allFramedItems)
            {
                int frameIndex = grouping.Key;
                IList<IFramedItem> itemSet = organizedFrames[frameIndex - minFrame];
                foreach (var item in grouping)
                {
                    item.MergeIntoFramedItemListSameFrame(ref itemSet, false, mergeThreshold, nameBoost);
                }
            }
            return organizedFrames;
        }

        /// <summary>
        ///   Places the items from an unsorted set into a buffer that is sorted by frame number.
        /// </summary>
        /// <param name="buffer">
        ///   The sorted buffer to add items to.
        /// </param>
        /// <param name="unsortedSet">
        ///   The unsorted set of items to add.
        /// </param>
        /// <returns>
        ///   Returns the merged, sorted set of combined items.
        /// </returns>
        public static IList<IList<IFramedItem>> InsertIntoSortedBuffer(IList<IList<IFramedItem>> buffer, IList<IFramedItem> unsortedSet, double mergeThreshold, double nameBoost)
        {
            if (unsortedSet.Count == 0)
            {
                return buffer;
            }
            /*
            buffer.Add(unsortedSet);
            buffer = GroupByFrame(buffer, mergeThreshold, nameBoost);
            return buffer;*/
            if (buffer.Count == 0)
            {
                buffer.Add(unsortedSet);
                return GroupByFrame(buffer, mergeThreshold, nameBoost);
            }

            bool sameFrame = AreAllSameFrame(unsortedSet);
            int bmin = buffer[0][0].Frame.FrameIndex;
            int bmax = buffer[^1][0].Frame.FrameIndex;
            int min = bmin;
            int max = bmax;

            for (int i = 0; i < unsortedSet.Count; i++)
            {
                int index = unsortedSet[i].Frame.FrameIndex;
                min = Math.Min(min, index);
                max = Math.Max(max, index);
            }

            IList<IList<IFramedItem>> destList;
            if (bmin == min)
            {
                destList = buffer;
            }
            else
            {
                destList = new List<IList<IFramedItem>>(max - min + 1);
            }
            for (int i = min + destList.Count; i <= max; i++)
            {
                if (sameFrame && unsortedSet[0].Frame.FrameIndex == i)
                {
                    destList.Add(unsortedSet);
                    return destList;
                }
                destList.Add(new List<IFramedItem>());
            }

            HashSet<int> shortcutFrames = new HashSet<int>();
            if (sameFrame)
            {
                int index = unsortedSet[0].Frame.FrameIndex - min;
            }

            foreach (var item in unsortedSet)
            {
                int tgtIndex = item.Frame.FrameIndex - min;
                IList<IFramedItem> dst = destList[tgtIndex];
                if (item.Frame.FrameIndex > bmax)
                {
                    dst.Add(item);
                    continue;
                }

                if (dst.Count == 0)
                {
                    shortcutFrames.Add(tgtIndex);
                }
                else if (dst.Count == 1 && dst[0] is FillerID)
                {
                    dst.RemoveAt(0);
                    shortcutFrames.Add(tgtIndex);
                }
                if (shortcutFrames.Contains(tgtIndex))
                {
                    dst.Add(item);
                }
                else
                {
                    item.MergeIntoFramedItemList(ref dst, false, mergeThreshold, nameBoost);
                }
            }
            return destList;
        }

        /// <summary>
        ///   Fills in an <see cref="IItemPath"/> to ensure that there are no missing frames.
        /// </summary>
        /// <param name="path">
        ///   The path to fill in.
        /// </param>
        /// <param name="frameBuffer">
        ///   The frames available to fill in the path.
        /// </param>
        public static void SealPath(IItemPath path, IList<IList<IFramedItem>> frameBuffer)
        {
            //var orgFrames = GroupByFrame(frameBuffer);
            var orgFrames = new List<IList<IFramedItem>>();
            for (int i = 0; i < frameBuffer.Count; i++)
            {
                orgFrames.Add(new List<IFramedItem>(frameBuffer[i]));
            }
            int x = 0;
            RemoveUsedFrames(path, orgFrames, ref x);
            int minFrame = int.MaxValue;
            int maxFrame = int.MinValue;
            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                int frameNum = path.FrameIndex(i);
                minFrame = Math.Min(minFrame, frameNum);
                maxFrame = Math.Max(maxFrame, frameNum);
            }

            for (int i = 0; i < orgFrames.Count; i++)
            {
                var frameGroup = orgFrames[i];
                for (int j = 0; j < frameGroup.Count; j++)
                {
                    IFrame f = frameGroup[j].Frame;
                    int fgNum = frameGroup[j].Frame.FrameIndex;
                    if (fgNum > minFrame && fgNum < maxFrame)
                    {
                        path.FramedItems.Add(new FramedItem(f, new FillerID()));
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///   Checks the provided list of items and selects up to one to add into the <see cref="IItemPath"/>.
        /// </summary>
        /// <param name="itemsInFrame">
        ///   A set of items, usually all in a single frame, to check.
        /// </param>
        /// <param name="predictor">
        ///   The <see cref="IPathPredictor"/> to use to test the items.
        /// </param>
        /// <param name="itemPath">
        ///   The <see cref="IItemPath"/> to add entries to.
        /// </param>
        /// <param name="similarityThreshold">
        ///   The threshold required to include an <see cref="IFramedItem"/> from the buffer in the path when compared to a predicted location in the same frame.
        /// </param>
        /// <returns>
        ///   Returns <see langword="true"/> if an item was added; <see langword="false"/> otherwise.
        /// </returns>
        public static bool TestAndAdd(IList<IFramedItem> itemsInFrame, IPathPredictor predictor, IItemPath itemPath, double similarityThreshold)
        {
            if (itemsInFrame.Count == 0)
            {
                return false;
            }

            int frameIndex = itemsInFrame.First().Frame.FrameIndex;
            Rectangle prediction = predictor.Predict(itemPath, frameIndex);

            double bestSim = similarityThreshold - 1;
            int closestIndex = -1;
            for (int j = 0; j < itemsInFrame.Count; j++)
            {
                var fItem = itemsInFrame[j];
                double sim = fItem.Similarity(prediction);
                if (sim > bestSim)
                {
                    bestSim = sim;
                    closestIndex = j;
                }
            }

            if (bestSim > similarityThreshold)
            {
                itemPath.FramedItems.Add(itemsInFrame[closestIndex]);
                return true;
            }
            return false;
        }

        /// <summary>
        ///   Attempts to merge paths together when they share a common <see cref="IItemID"/>.
        /// </summary>
        /// <param name="paths">
        ///   The set of paths to merge together.
        /// </param>
        public static void TryMergePaths(ref IList<IItemPath> paths, double similarityThreshold)
        {
            IList<IItemPath> outPaths = new List<IItemPath>();

            Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries = new Dictionary<IItemPath, (int minFrame, int maxFrame)>();

            foreach (var path in paths)
            {
                boundaries.Add(path, path.GetPathBounds());
            }
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                int minFrame;
                int maxFrame;
                (minFrame, maxFrame) = boundaries[path];

                for (int j = i + 1; j < paths.Count; j++)
                {
                    var tgtPath = paths[j];
                    int tgtMin;
                    int tgtMax;

                    (tgtMin, tgtMax) = boundaries[path];

                    if (tgtMax - maxFrame > 250)
                    {
                        break;
                    }

                    for (int k = Math.Max(tgtMin, minFrame); k < Math.Min(tgtMax, maxFrame); k++)
                    {
                        var tgtFi = GetFramedItemByFrameNumber(tgtPath, k);
                        if (tgtFi == null)
                        {
                            continue;
                        }
                        var srcFi = GetFramedItemByFrameNumber(path, k);
                        if (srcFi == null)
                        {
                            continue;
                        }
                        if (AreFramedItemsMatched(srcFi, tgtFi, similarityThreshold))
                        {
                            foreach (var fi in tgtPath.FramedItems)
                            {
                                var dest = GetFramedItemByFrameNumber(path, fi.Frame.FrameIndex);
                                if (dest == null)
                                {
                                    path.FramedItems.Add(fi);
                                }
                                else
                                {
                                    foreach (var id in fi.ItemIDs)
                                    {
                                        if (dest.ItemIDs.Contains(id))
                                        {
                                            continue;
                                        }
                                        else if (HasSimilarID(id, dest.ItemIDs))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            dest.ItemIDs.Add(id);
                                        }
                                    }
                                }
                            }
                            maxFrame = Math.Max(maxFrame, tgtMax);
                            minFrame = Math.Min(minFrame, tgtMin);
                            ++i;
                        }
                    }
                }
                outPaths.Add(path);
            }

            paths = outPaths;
        }

        /// <summary>
        ///   Attempts to merge paths together when they share a common <see cref="IItemID"/>.
        /// </summary>
        /// <param name="paths">
        ///   The set of paths to merge together.
        /// </param>
        public void TryMergePaths(ref IList<IItemPath> paths, float similarityThreshold)
        {
            IList<IItemPath> outPaths = new List<IItemPath>();
            var boundaries = GetPathBoundaries(paths);

            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                (int minFrame, int maxFrame) = boundaries[path];
                bool pathMerged;

                do
                {
                    pathMerged = false;
                    for (int j = i + 1; j < paths.Count; j++)
                    {
                        int matchCount = 0;
                        var tgtPath = paths[j];
                        (int tgtMin, int tgtMax) = boundaries[path];
                        int gapBetween = FrameGap(path, tgtPath, boundaries);
                        if (gapBetween > 250)
                        {
                            break;
                        }

                        var overlapRange = FindPathOverlap(path, tgtPath, boundaries);

                        int startPoint;
                        int endPoint;

                        if (gapBetween > 0)
                        {
                            startPoint = overlapRange.maxOverlap;
                            endPoint = overlapRange.minOverlap;
                        }
                        else if (gapBetween == 0)
                        {
                            startPoint = overlapRange.minOverlap - 3;
                            endPoint = overlapRange.maxOverlap + 3;
                        }
                        else
                        {
                            startPoint = overlapRange.minOverlap;
                            endPoint = overlapRange.maxOverlap;
                        }

                        for (int k = startPoint; k < endPoint; k++)
                        {
                            var tgtFi = GetFramedItemByFrameNumber(tgtPath, k);
                            var srcFi = GetFramedItemByFrameNumber(path, k);

                            if (tgtFi == null || srcFi == null)
                            {
                                matchCount += PredictiveMatchCount(path, tgtPath, k, similarityThreshold);
                            }

                            if (matchCount > 5 || AreFramedItemsMatched(srcFi, tgtFi, similarityThreshold))
                            {
                                MergePaths(boundaries, path, ref minFrame, ref maxFrame, tgtPath, tgtMin, tgtMax);
                                paths.RemoveAt(j);
                                pathMerged = true;
                                j--;
                                break;
                            }
                        }
                    }
                } while (pathMerged)
                    ;
                outPaths.Add(path);
            }

            paths = outPaths;
        }

        private static Dictionary<IItemPath, (int minFrame, int maxFrame)> GetPathBoundaries(IList<IItemPath> paths)
        {
            Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries = new Dictionary<IItemPath, (int minFrame, int maxFrame)>();

            foreach (var path in paths)
            {
                boundaries.Add(path, path.GetPathBounds());
            }

            return boundaries;
        }

        private static void MergePaths(Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries, IItemPath path, ref int minFrame, ref int maxFrame, IItemPath tgtPath, int tgtMin, int tgtMax)
        {
            foreach (var fi in tgtPath.FramedItems)
            {
                var dest = GetFramedItemByFrameNumber(path, fi.Frame.FrameIndex);
                if (dest == null)
                {
                    path.FramedItems.Add(fi);
                }
                else
                {
                    foreach (var id in fi.ItemIDs)
                    {
                        if (dest.ItemIDs.Contains(id))
                        {
                            continue;
                        }
                        else if (HasSimilarID(id, dest.ItemIDs))
                        {
                            continue;
                        }
                        else
                        {
                            dest.ItemIDs.Add(id);
                        }
                    }
                }
            }
            maxFrame = Math.Max(maxFrame, tgtMax);
            minFrame = Math.Min(minFrame, tgtMin);
            boundaries[path] = (minFrame, maxFrame);
        }

        private (int minOverlap, int maxOverlap) FindPathOverlap(IItemPath path1, IItemPath path2, Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries)
        {
            int minFrame1;
            int maxFrame1;
            int minFrame2;
            int maxFrame2;
            (minFrame1, maxFrame1) = boundaries[path1];
            (minFrame2, maxFrame2) = boundaries[path2];

            int rangeMin = Math.Max(minFrame1, minFrame2);
            int rangeMax = Math.Min(maxFrame1, maxFrame2);
            return (rangeMin, rangeMax);
        }

        private int FrameGap(IItemPath path1, IItemPath path2, Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries)
        {
            int minFrame1;
            int maxFrame1;
            int minFrame2;
            int maxFrame2;
            (minFrame1, maxFrame1) = boundaries[path1];
            (minFrame2, maxFrame2) = boundaries[path2];

            int rangeMin = Math.Max(minFrame1, minFrame2);
            int rangeMax = Math.Min(maxFrame1, maxFrame2);
            return rangeMin - rangeMax;
        }

        private int PredictiveMatchCount(IItemPath path1, IItemPath path2, int frameIndex, float similarityThreshold)
        {
            var fi1 = GetFramedItemByFrameNumber(path1, frameIndex);
            var fi2 = GetFramedItemByFrameNumber(path2, frameIndex);
            int matchCount = 0;

            if (fi1 == null || fi2 == null)
            {
                for (int m = 0; m < Predictors.Count; m++)
                {
                    (var predictor, var threshold) = Predictors[m];
                    threshold = (threshold + 1.0f + similarityThreshold) / (3.0f);
                    Rectangle r1 = fi1?.MeanBounds.RoundRectF() ?? predictor.Predict(path1, frameIndex);
                    Rectangle r2 = fi2?.MeanBounds.RoundRectF() ?? predictor.Predict(path2, frameIndex);
                    if (r1.IntersectionOverUnion(r2) > threshold)
                    {
                        matchCount++;
                    }
                }
            }

            return matchCount;
        }

        /// <summary>
        ///   Attempts to merge paths together when they share a common <see cref="IItemID"/>.
        /// </summary>
        /// <param name="paths">
        ///   The set of paths to merge together.
        /// </param>
        public static void TryMergePaths(ref IList<IItemPath> paths, IPathPredictor predictor, double similarityThreshold)
        {
            IList<IItemPath> outPaths = new List<IItemPath>();

            Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries = new Dictionary<IItemPath, (int minFrame, int maxFrame)>();

            foreach (var path in paths)
            {
                boundaries.Add(path, path.GetPathBounds());
            }
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                int minFrame;
                int maxFrame;
                (minFrame, maxFrame) = boundaries[path];

                for (int j = paths.Count - 1; j > i; --j)
                {
                    var tgtPath = paths[j];
                    int tgtMin;
                    int tgtMax;

                    int matchCount = 0;
                    (tgtMin, tgtMax) = boundaries[path];

                    if (tgtMax - maxFrame > 250)
                    {
                        break;
                    }

                    for (int k = Math.Max(tgtMin, minFrame); k < Math.Min(tgtMax, maxFrame); k++)
                    {
                        var tgtFi = GetFramedItemByFrameNumber(tgtPath, k);
                        var srcFi = GetFramedItemByFrameNumber(path, k);
                        if (tgtFi == null)
                        {
                            if (srcFi == null)
                            {
                                Rectangle r1 = predictor.Predict(tgtPath, k);
                                Rectangle r2 = predictor.Predict(path, k);
                                if (r1.IntersectionOverUnion(r2) > 0.85)
                                {
                                    matchCount++;
                                }
                            }
                            else
                            {
                                Rectangle r = predictor.Predict(tgtPath, k);
                                if (r.IntersectionOverUnion(srcFi.MeanBounds.RoundRectF()) > 0.85)
                                {
                                    matchCount++;
                                }
                            }
                        }
                        else if (srcFi == null)
                        {
                            Rectangle r = predictor.Predict(path, k);
                            if (r.IntersectionOverUnion(srcFi.MeanBounds.RoundRectF()) > 0.85)
                            {
                                matchCount++;
                            }
                        }
                        if (matchCount > 5 || AreFramedItemsMatched(srcFi, tgtFi, similarityThreshold))
                        {
                            foreach (var fi in tgtPath.FramedItems)
                            {
                                var dest = GetFramedItemByFrameNumber(path, fi.Frame.FrameIndex);
                                if (dest == null)
                                {
                                    path.FramedItems.Add(fi);
                                }
                                else
                                {
                                    foreach (var id in fi.ItemIDs)
                                    {
                                        if (dest.ItemIDs.Contains(id))
                                        {
                                            continue;
                                        }
                                        else if (HasSimilarID(id, dest.ItemIDs))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            dest.ItemIDs.Add(id);
                                        }
                                    }
                                }
                            }
                            maxFrame = Math.Max(maxFrame, tgtMax);
                            minFrame = Math.Min(minFrame, tgtMin);
                            paths.RemoveAt(j);
                            break;
                        }
                    }
                }
                outPaths.Add(path);
            }

            paths = outPaths;
        }

        public IItemPath BuildPath(IFramedItem framedID, IList<IList<IFramedItem>> buffer, bool suppressOutput)
        {
            IItemPath itemPath = new ItemPath();
            itemPath.FramedItems.Add(framedID);

            if( !SetupBuilder(framedID,
                              buffer,
                              itemPath,
                              out var givenIndex,
                              out var orgFrames,
                              out var filterTable,
                              out var minFrame,
                              out var maxFrame,
                              out var startingIndex,
                              out var counts))
            {
                return null;
            }

            if(filterTable.ContainsKey(framedID)&&!filterTable[framedID])
            {
                return null;
            }

            if(orgFrames.Count == 0)
            {
                return itemPath;
            }

            bool matchFound;
            do
            {
                matchFound = false;
                int startCount = itemPath.FramedItems.Count;
                for (int i = 0; i < Predictors.Count && orgFrames.Count > 0; i++)
                {
                    (var predictor, var threshold) = Predictors[i];
                    for (int j = 0; j < ThresholdCoefProgression.Count && orgFrames.Count > 0; j++)
                    {
                        if (RunInductionPass(predictor, itemPath, ThresholdCoefProgression[j], threshold, orgFrames, ref startingIndex, counts, suppressOutput, maxFrame))
                        {
                            break;
                        }
                    }
                }
                int endCount = itemPath.FramedItems.Count;
                matchFound = startCount != endCount;
            } while (matchFound && orgFrames.Count > 0)
                ;

            return itemPath;
        }
        public IItemPath ExtendPath(IItemPath itemPath, IList<IList<IFramedItem>> buffer, bool suppressOutput)
        {
            SetupExtender(itemPath,
                          buffer,
                          out var orgFrames,
                          out var filterTable,
                          out var minFrame,
                          out var maxFrame,
                          out var startingIndex,
                          out var counts);

            bool matchFound = false;
            do
            {
                int startCount = itemPath.FramedItems.Count;
                for (int i = 0; i < Predictors.Count && orgFrames.Count > 0; i++)
                {
                    (var predictor, var threshold) = Predictors[i];
                    for (int j = 0; j < ThresholdCoefProgression.Count && orgFrames.Count > 0; j++)
                    {
                        if (RunInductionPass(predictor, itemPath, ThresholdCoefProgression[j], threshold, orgFrames, ref startingIndex, counts, suppressOutput, maxFrame))
                        {
                            break;
                        }
                    }
                }
                int endCount = itemPath.FramedItems.Count;
                matchFound = startCount != endCount;
            } while (matchFound && orgFrames.Count > 0)
                ;

            return itemPath;
        }

        /// <summary>
        ///   Given an <see cref="IItemPath"/>, attempt to expand it using the provided
        ///   frame buffer. This function is usually used to build paths in stages using
        ///   different <see cref="IPathPredictor">IPathPredictors</see>.
        /// </summary>
        /// <param name="itemPath">
        ///   The existing <see cref="IItemPath"/> to try to expand.
        /// </param>
        /// <param name="buffer">
        ///   The buffer of frames available to expand the path.
        /// </param>
        /// <param name="predictor">
        ///   The <see cref="IPathPredictor"/> to use to expand the path.
        /// </param>
        /// <param name="similarityThreshold">
        ///   The threshold required to include an <see cref="IFramedItem"/> from the buffer in the path when compared to a predicted location in the same frame.
        /// </param>
        /// <returns>
        ///   Returns the expanded <see cref="IItemPath"/>.
        /// </returns>
        public IItemPath ExpandPathFromBuffer(IItemPath itemPath, IList<IList<IFramedItem>> buffer, IPathPredictor predictor, float similarityThreshold)
        {
            return ExpandPathFromBuffer(itemPath, buffer, predictor, similarityThreshold, false);
        }

        /// <summary>
        ///   Given an <see cref="IItemPath"/>, attempt to expand it using the provided
        ///   frame buffer. This function is usually used to build paths in stages using
        ///   different <see cref="IPathPredictor">IPathPredictors</see>.
        /// </summary>
        /// <param name="itemPath">
        ///   The existing <see cref="IItemPath"/> to try to expand.
        /// </param>
        /// <param name="buffer">
        ///   The buffer of frames available to expand the path.
        /// </param>
        /// <param name="predictor">
        ///   The <see cref="IPathPredictor"/> to use to expand the path.
        /// </param>
        /// <param name="similarityThreshold">
        ///   The threshold required to include an <see cref="IFramedItem"/> from the buffer in the path when compared to a predicted location in the same frame.
        /// </param>
        /// <returns>
        ///   Returns the expanded <see cref="IItemPath"/>.
        /// </returns>
        public IItemPath ExpandPathFromBuffer(IItemPath itemPath, IList<IList<IFramedItem>> buffer, IPathPredictor predictor, float similarityThreshold, bool suppressOutput)
        {
            if (buffer.Count == 0)
            {
                return itemPath;
            }

            similarityThreshold = Math.Max(similarityThreshold, 0.0f);

            int givenIndex = itemPath.FramedItems.First().Frame.FrameIndex;

            // Sort the contents of the buffer by frame index.
            // var orgFrames = GroupByFrame(buffer);
            IList<IList<IFramedItem>> orgFrames = new List<IList<IFramedItem>>();
            for (int i = 0; i < buffer.Count; i++)
            {
                orgFrames.Add(new List<IFramedItem>(buffer[i]));
            }

            var filterTable = GetFilterTable(orgFrames, itemPath);
            FilterCache(orgFrames, filterTable);

            int minFrame = orgFrames[0][0].Frame.FrameIndex;
            int maxFrame = orgFrames[^1][0].Frame.FrameIndex;

            int startingIndex = givenIndex - minFrame;
            List<int> counts = new List<int>();

            RemoveUsedFrames(itemPath, orgFrames, ref startingIndex);

            if (orgFrames.Count == 0)
            {
                LatestAnalyzer[itemPath] = predictor;
                LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;
                return itemPath;
            }

            for (int i = 0; i < ThresholdCoefProgression.Count; i++)
            {
                if (RunInductionPass(predictor, itemPath, ThresholdCoefProgression[i], similarityThreshold, orgFrames, ref startingIndex, counts, suppressOutput, maxFrame))
                {
                    return itemPath;
                }
            }
            RunInductionPass(predictor, itemPath, 0.0f, similarityThreshold, orgFrames, ref startingIndex, counts, suppressOutput, maxFrame);

            if (!suppressOutput)
            {
                counts.Add(itemPath.FramedItems.Count);

                for (int i = 0; i < counts.Count; i++)
                {
                    Console.WriteLine("Pass " + (i + 1) + ": " + counts[i]);
                }
            }

            LatestAnalyzer[itemPath] = predictor;
            LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;
            return itemPath;
        }

        private bool RunInductionPass(IPathPredictor predictor,
                                      IItemPath itemPath,
                                      float iouFactor,
                                      float simThreshold,
                                      IList<IList<IFramedItem>> orgFrames,
                                      ref int startingIndex,
                                      IList<int> counts,
                                      bool suppressOutput,
                                      int latestFrame)
        {
            InductionPass(predictor, (simThreshold + iouFactor) / (1.0f + iouFactor), itemPath, orgFrames, ref startingIndex);
            if (!suppressOutput)
            {
                counts.Add(itemPath.FramedItems.Count);
            }
            if (orgFrames.Count == 0)
            {
                LatestAnalyzer[itemPath] = predictor;
                LatestFrameAnalyzed[itemPath] = latestFrame;
                return true;
            }
            return false;
        }


        /// <summary>
        ///   Builds a path using the provided positive id, buffer, and prediction method.
        /// </summary>
        /// <param name="framedID">
        ///   The primary <see cref="IFramedItem"/> to build a path from.
        /// </param>
        /// <param name="buffer">
        ///   A buffer of <see cref="IFramedItem">IFramedItems</see> available to build the path from.
        /// </param>
        /// <param name="predictor">
        ///   The <see cref="IPathPredictor"/> used to predict the location of an item in some other frame.
        /// </param>
        /// <param name="similarityThreshold">
        ///   The threshold required to include an <see cref="IFramedItem"/> from the buffer in the path when compared to a predicted location in the same frame.
        /// </param>
        /// <returns>
        ///   Returns the <see cref="IItemPath"/> representing the path of the object through some series of frames.
        /// </returns>
        public IItemPath GetPathFromIdAndBuffer(IFramedItem framedID, IList<IList<IFramedItem>> buffer, IPathPredictor predictor, float similarityThreshold)
        {
            return GetPathFromIdAndBuffer(framedID, buffer, predictor, similarityThreshold, false);
        }

        private bool SetupBuilder(IFramedItem framedID,
                                  IList<IList<IFramedItem>> buffer,
                                  IItemPath itemPath,
                                  out int givenIndex,
                                  out IList<IList<IFramedItem>> orgFrames,
                                  out IDictionary<IFramedItem, bool> filterTable,
                                  out int minFrame,
                                  out int maxFrame,
                                  out int startingIndex,
                                  out List<int> counts)
        {

            givenIndex = framedID.Frame.FrameIndex;
            // Sort the contents of the buffer by frame index.
            // var orgFrames = GroupByFrame(buffer);
            orgFrames = new List<IList<IFramedItem>>();
            for (int i = 0; i < buffer.Count; i++)
            {
                orgFrames.Add(new List<IFramedItem>(buffer[i]));
            }

            orgFrames = EnsurePathPresenceInCache(framedID, givenIndex, orgFrames);
            minFrame = orgFrames[0][0].Frame.FrameIndex;
            maxFrame = orgFrames[^1][0].Frame.FrameIndex;
            filterTable = GetFilterTable(orgFrames, itemPath);
            FilterCache(orgFrames, filterTable);
            counts = new List<int>();

            if (orgFrames.Count == 0)
            {
                startingIndex = 0;
                return false;
            }



            startingIndex = givenIndex - minFrame;
            startingIndex = Math.Max(0, startingIndex);
            startingIndex = Math.Min(orgFrames.Count - 1, startingIndex);
            RemoveUsedFrames(itemPath, orgFrames, ref startingIndex);
            return true;
        }

        private void SetupExtender(IItemPath itemPath,
                                   IList<IList<IFramedItem>> buffer,
                                   out IList<IList<IFramedItem>> orgFrames,
                                   out IDictionary<IFramedItem, bool> filterTable,
                                   out int minFrame,
                                   out int maxFrame,
                                   out int startingIndex,
                                   out List<int> counts)
        {
            int givenIndex = itemPath.FramedItems.First().Frame.FrameIndex;

            // Sort the contents of the buffer by frame index.
            // var orgFrames = GroupByFrame(buffer);
            orgFrames = new List<IList<IFramedItem>>();
            for (int i = 0; i < buffer.Count; i++)
            {
                orgFrames.Add(new List<IFramedItem>(buffer[i]));
            }

            minFrame = orgFrames[0][0].Frame.FrameIndex;
            maxFrame = orgFrames[^1][0].Frame.FrameIndex;

            filterTable = GetFilterTable(orgFrames, itemPath);
            FilterCache(orgFrames, filterTable);



            startingIndex = givenIndex - minFrame;
            startingIndex = Math.Max(0, startingIndex);
            startingIndex = Math.Min(orgFrames.Count - 1, startingIndex);
            RemoveUsedFrames(itemPath, orgFrames, ref startingIndex);
            counts = new List<int>();
        }

        /// <summary>
        ///   Builds a path using the provided positive id, buffer, and prediction method.
        /// </summary>
        /// <param name="framedID">
        ///   The primary <see cref="IFramedItem"/> to build a path from.
        /// </param>
        /// <param name="buffer">
        ///   A buffer of <see cref="IFramedItem">IFramedItems</see> available to build the path from.
        /// </param>
        /// <param name="predictor">
        ///   The <see cref="IPathPredictor"/> used to predict the location of an item in some other frame.
        /// </param>
        /// <param name="similarityThreshold">
        ///   The threshold required to include an <see cref="IFramedItem"/> from the buffer in the path when compared to a predicted location in the same frame.
        /// </param>
        /// <returns>
        ///   Returns the <see cref="IItemPath"/> representing the path of the object through some series of frames.
        /// </returns>
        public IItemPath GetPathFromIdAndBuffer(IFramedItem framedID, IList<IList<IFramedItem>> buffer, IPathPredictor predictor, float similarityThreshold, bool suppressOutput)
        {
            IItemPath itemPath = new ItemPath();
            itemPath.FramedItems.Add(framedID);
            if (buffer.Count == 0)
            {
                return itemPath;
            }
            int givenIndex = framedID.Frame.FrameIndex;
            // Sort the contents of the buffer by frame index.
            // var orgFrames = GroupByFrame(buffer);
            IList<IList<IFramedItem>> orgFrames = new List<IList<IFramedItem>>();
            for (int i = 0; i < buffer.Count; i++)
            {
                orgFrames.Add(new List<IFramedItem>(buffer[i]));
            }

            orgFrames = EnsurePathPresenceInCache(framedID, givenIndex, orgFrames);
            var filterTable = GetFilterTable(orgFrames, itemPath);
            FilterCache(orgFrames, filterTable);

            if (orgFrames.Count == 0)
            {
                return null;
            }

            int minFrame = orgFrames[0][0].Frame.FrameIndex;
            int maxFrame = orgFrames[^1][0].Frame.FrameIndex;

            int startingIndex = givenIndex - minFrame;
            startingIndex = Math.Max(0, startingIndex);
            startingIndex = Math.Min(orgFrames.Count - 1, startingIndex);
            List<int> counts = new List<int>();
            for (int i = 0; i < ThresholdCoefProgression.Count; i++)
            {
                if (RunInductionPass(predictor, itemPath, ThresholdCoefProgression[i], similarityThreshold, orgFrames, ref startingIndex, counts, suppressOutput, maxFrame))
                {
                    return itemPath;
                }
            }
            RunInductionPass(predictor, itemPath, 0.0f, similarityThreshold, orgFrames, ref startingIndex, counts, suppressOutput, maxFrame);

            /*InductionPass(predictor, (similarityThreshold + 0.5) / 1.5, itemPath, orgFrames, ref startingIndex);
            if (orgFrames.Count == 0)
            {
                LatestAnalyzer[itemPath] = predictor;
                LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;
                return itemPath;
            }
            counts.Add(itemPath.FramedItems.Count);

            InductionPass(predictor, (similarityThreshold + 0.3) / 1.3, itemPath, orgFrames, ref startingIndex);

            if (orgFrames.Count == 0)
            {
                LatestAnalyzer[itemPath] = predictor;
                LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;
                return itemPath;
            }
            counts.Add(itemPath.FramedItems.Count);

            InductionPass(predictor, (similarityThreshold + 0.1) / 1.1, itemPath, orgFrames, ref startingIndex);

            if (orgFrames.Count == 0)
            {
                LatestAnalyzer[itemPath] = predictor;
                LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;
                return itemPath;
            }
            counts.Add(itemPath.FramedItems.Count);

            InductionPass(predictor, similarityThreshold, itemPath, orgFrames, ref startingIndex);
            counts.Add(itemPath.FramedItems.Count);*/
            /*for ( int i = 0; i < Math.Max( startingIndex, orgFrames.Last().First().Frame.FrameIndex - givenIndex ); ++i )
            {
                if ( ( startingIndex + i ) < orgFrames.Count )
                {
                    TestAndAdd( orgFrames[startingIndex + i], predictor, itemPath, similarityThreshold );
                }
                if ( ( startingIndex - i ) >= 0 && i != 0 )
                {
                    TestAndAdd( orgFrames[startingIndex - i], predictor, itemPath, similarityThreshold );
                }
            }
            for ( int i = framedID.Frame.FrameIndex - minFrame - 1; i >= 0; --i )
            {
                TestAndAdd( orgFrames[i], predictor, itemPath, similarityThreshold );
            }
            for ( int i = framedID.Frame.FrameIndex - minFrame + 1; i < orgFrames.Count; ++i )
            {
                TestAndAdd( orgFrames[i], predictor, itemPath, similarityThreshold );
            }*/

            /*LatestAnalyzer[itemPath] = predictor;
            LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;*/

            for (int i = 0; i < counts.Count; i++)
            {
                Console.WriteLine("Pass " + (i + 1) + ": " + counts[i]);
            }
            if (itemPath.FramedItems.Count == 1 && !filterTable[itemPath.FramedItems[0]])
            {
                return null;
            }
            return itemPath;
        }

        /// <summary>
        ///   Given an <see cref="IItemPath"/>, attempt to expand it using the provided
        ///   frame buffer. This function is usually used to build paths in stages using
        ///   different <see cref="IPathPredictor">IPathPredictors</see>.
        /// </summary>
        /// <param name="itemPath">
        ///   The existing <see cref="IItemPath"/> to try to expand.
        /// </param>
        /// <param name="buffer">
        ///   The buffer of frames available to expand the path.
        /// </param>
        /// <param name="predictor">
        ///   The <see cref="IPathPredictor"/> to use to expand the path.
        /// </param>
        /// <param name="similarityThreshold">
        ///   The threshold required to include an <see cref="IFramedItem"/> from the buffer in the path when compared to a predicted location in the same frame.
        /// </param>
        /// <returns>
        ///   Returns the expanded <see cref="IItemPath"/>.
        /// </returns>
        public IItemPath QuickExpandPathFromBuffer(IItemPath itemPath, IList<IList<IFramedItem>> buffer, IPathPredictor predictor, double similarityThreshold)
        {
            if (buffer.Count == 0)
            {
                return itemPath;
            }
            if (similarityThreshold < 0)
            {
                similarityThreshold = 0;
            }
            int givenIndex = itemPath.FramedItems.First().Frame.FrameIndex;

            // Sort the contents of the buffer by frame index.
            // var orgFrames = GroupByFrame(buffer);
            IList<IList<IFramedItem>> orgFrames = new List<IList<IFramedItem>>();
            for (int i = 0; i < buffer.Count; i++)
            {
                orgFrames.Add(new List<IFramedItem>(buffer[i]));
            }
            var filterTable = GetFilterTable(orgFrames, itemPath);
            FilterCache(orgFrames, filterTable);

            int minFrame = orgFrames[0][0].Frame.FrameIndex;

            int startingIndex = givenIndex - minFrame;

            RemoveUsedFrames(itemPath, orgFrames, ref startingIndex);
            if (orgFrames.Count == 0)
            {
                return itemPath;
            }

            InductionPass(predictor, similarityThreshold, itemPath, orgFrames, ref startingIndex);

            LatestAnalyzer[itemPath] = predictor;
            LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;
            return itemPath;
        }

        private static bool AreAllSameFrame(IList<IFramedItem> unsortedSet)
        {
            bool sameFrame = true;
            if(unsortedSet == null || unsortedSet.Count == 0)
            {
                return true;
            }
            var f = unsortedSet.First().Frame;
            int frame = f.FrameIndex;
            for (int i = 0; i < unsortedSet.Count; i++)
            {
                var f2 = unsortedSet[i].Frame;
                if (f != f2)
                {
                    if (unsortedSet[i].Frame.FrameIndex != frame)
                    {
                        sameFrame = false;
                        break;
                    }
                    else
                    {
                        // This is unexpected;
                        Console.WriteLine("Duplicate frame found?");
                    }
                }
            }

            return sameFrame;
        }

        private static bool AreFramedItemsMatched(IFramedItem item1, IFramedItem item2, double similarityThreshold)
        {
            if (item1 is null || item2 is null)
            {
                return false;
            }
            if (item1 == item2)
            {
                return true;
            }
            if (item1.Frame.FrameIndex != item2.Frame.FrameIndex || item1.ItemIDs.Last().SourceObject == item2.ItemIDs.Last().SourceObject)
            {
                return false;
            }
            for (int i = 0; i < item1.ItemIDs.Count; i++)
            {
                var id1 = item1.ItemIDs[i];
                for (int j = 0; j < item2.ItemIDs.Count; j++)
                {
                    var id2 = item2.ItemIDs[j];

                    if (id1.Confidence == id2.Confidence && id1.Confidence > 0)
                    {
                        return true;
                    }
                    if (id1.BoundingBox.Location == id2.BoundingBox.Location && id1.BoundingBox.Size == id2.BoundingBox.Size)
                    {
                        return true;
                    }
                }
            }
            if (item1.Similarity(item2.MeanBounds) >= similarityThreshold)
            {
                return true;
            }
            return false;
        }

        private static IList<IList<IFramedItem>> EnsurePathPresenceInCache(IFramedItem framedID, int givenIndex, IList<IList<IFramedItem>> orgFrames)
        {
            int baseFrame = orgFrames[0][0].Frame.FrameIndex;
            if (orgFrames.Count > givenIndex - baseFrame && givenIndex >= baseFrame && !orgFrames[givenIndex - baseFrame].Contains(framedID))
            {
                orgFrames[givenIndex - baseFrame].Add(framedID);
            }
            else if (orgFrames.Count <= givenIndex - baseFrame)
            {
                List<IFramedItem> finalList = new List<IFramedItem>() { framedID };
                orgFrames.Add(finalList);
            }
            else if (givenIndex - baseFrame < 0)
            {
                int numtoadd = baseFrame - givenIndex;

                List<IFramedItem> finalList = new List<IFramedItem>() { framedID };
                IList<IList<IFramedItem>> reorg = new List<IList<IFramedItem>>();
                for (int i = 0; i < numtoadd; i++)
                {
                    if (i == 0)
                    {
                        reorg.Add(finalList);
                    }
                    else
                    {
                        reorg.Add(new List<IFramedItem>());
                    }
                }
                for (int i = 0; i < orgFrames.Count; i++)
                {
                    reorg.Add(orgFrames[i]);
                }
                orgFrames = reorg;
            }

            return orgFrames;
        }
        private static IFramedItem GetFramedItemByFrameNumber(IItemPath path, int frameNumber)
        {
            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                var fi = path.FramedItems[i];
                if (fi.Frame.FrameIndex == frameNumber)
                {
                    return fi;
                }
            }
            return null;
        }

        private static bool HasSimilarID(IItemID id, IList<IItemID> idlist)
        {
            if (id is FillerID)
            {
                return true;
            }
            if (idlist.Count == 1 && idlist[0] is FillerID)
            {
                return false;
            }
            for (int i = 0; i < idlist.Count; i++)
            {
                if (id.BoundingBox == idlist[i].BoundingBox)
                {
                    return true;
                }
            }
            return false;
        }

        private static void RemoveUsedFrames(IItemPath itemPath, IList<IList<IFramedItem>> orgFrames, ref int startingIndex)
        {
            if (orgFrames == null || orgFrames.Count == 0)
            {
                return;
            }

            var usedFrames = from framedItem in itemPath.FramedItems
                             let frame = framedItem.Frame.FrameIndex
                             orderby frame
                             select frame;

            List<int> usedIndices = new List<int>(usedFrames);

            int startingFrameNumber = orgFrames[0][0].Frame.FrameIndex + startingIndex;

            /*int i = 0;
            foreach ( int used in usedFrames )
            {
                while ( i < orgFrames.Count )
                {
                    if ( orgFrames[i].Count == 0 ) // remove an empty frame
                    {
                        orgFrames.RemoveAt( i );
                        if ( i <= startingIndex )
                        {
                            --startingIndex;
                        }
                        continue;
                    }

                    int frameIndex = orgFrames[i].First().Frame.FrameIndex;

                    if ( frameIndex == used ) // remove an already present frame
                    {
                        orgFrames.RemoveAt( i );
                        if ( i <= startingIndex )
                        {
                            --startingIndex;
                        }
                        break;
                    }

                    // unused but populated frame
                    ++i;
                }
            }*/
            int i = 0;
            while (i < orgFrames.Count)
            {/*
                if (orgFrames[i].Count == 0)
                {
                    // Remove empty
                    if (i <= startingIndex && startingIndex > 0)
                    {
                        --startingIndex;
                    }
                    orgFrames.RemoveAt(i);
                    --i;
                    continue;
                }
                if (usedFrames.Contains(orgFrames[i].First().Frame.FrameIndex))
                {
                    if (i <= startingIndex && startingIndex > 0)
                    {
                        --startingIndex;
                    }
                    orgFrames.RemoveAt(i);
                    --i;
                }*/
                if (orgFrames[i].Count == 0)
                {
                    // Remove empty
                    if (i <= startingIndex && startingIndex > 0)
                    {
                        --startingIndex;
                    }
                    orgFrames.RemoveAt(i);
                    continue; // New item is now present at index i
                }
                int orgFramesFrameNumber = orgFrames[i][0].Frame.FrameIndex;
                if (usedIndices.BinarySearch(orgFramesFrameNumber) >= 0)
                {
                    // This index is already used.
                    if (i <= startingIndex && startingIndex > 0)
                    {
                        --startingIndex;
                    }
                    orgFrames.RemoveAt(i);
                    continue; // New item is now present at index i
                }
                ++i;
            }

            // Verify that the resulting startingIndex is the closest option to the initial starting frame number available.

            int bestStart = startingIndex;
            int bestStartDistance = 99999;

            for (int j = 0; j < orgFrames.Count; j++)
            {
                int frameNumber = orgFrames[j][0].Frame.FrameIndex;
                int distance = Math.Abs(startingFrameNumber - frameNumber);
                if (distance < bestStartDistance)
                {
                    bestStart = j;
                    bestStartDistance = distance;
                }
                if (distance == 0)
                {
                    break;
                }
            }
            startingIndex = bestStart;
        }

        private void FilterCache(IList<IList<IFramedItem>> cache, IDictionary<IFramedItem, bool> filterTable)
        {
            for (int i = 0; i < cache.Count; i++)
            {
                for (int j = cache[i].Count - 1; j >= 0; --j)
                {
                    if (!filterTable[cache[i][j]])
                    {
                        cache[i].RemoveAt(j);
                    }
                }
            }
            {
                int j = cache.Count - 1;
                while (cache.Count > 0 && cache[j].Count == 0)
                {
                    cache.RemoveAt(j);
                    --j;
                }
                while (cache.Count > 0 && cache[0].Count == 0)
                {
                    cache.RemoveAt(0);
                }
            }
        }

        private IDictionary<IFramedItem, bool> GetFilterTable(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            Dictionary<IFramedItem, bool> results = new();
            for (int i = 0; i < Filters.Count; i++)
            {
                var filterResult = Filters[i].FilterCache(cache, path);
                foreach (var entry in filterResult)
                {
                    if (results.ContainsKey(entry.Key) && !entry.Value)
                    {
                        results[entry.Key] = false;
                    }
                    else
                    {
                        results.Add(entry.Key, entry.Value);
                    }
                }
            }
            return results;
        }

        private void InductionPass(IPathPredictor predictor, double similarityThreshold, IItemPath itemPath, IList<IList<IFramedItem>> orgFrames, ref int startingIndex)
        {
            startingIndex = Math.Max(0, Math.Min(orgFrames.Count - 1, startingIndex));
            int lowIndex = Math.Max(0, startingIndex - 1);
            int highIndex = startingIndex;
            if (LatestAnalyzer.ContainsKey(itemPath) && LatestAnalyzer[itemPath] == predictor && orgFrames[highIndex][0].Frame.FrameIndex > LatestFrameAnalyzed[itemPath])
            {
                highIndex = orgFrames[highIndex][0].Frame.FrameIndex - orgFrames[0][0].Frame.FrameIndex;
            }
            while ((lowIndex >= 0 || highIndex < orgFrames.Count) && orgFrames.Count > 0)
            {
                int currentSize = itemPath.FramedItems.Count;
                for (; lowIndex >= 0 && currentSize > 0 && orgFrames.Count > 0; --lowIndex)
                {
                    if (LatestAnalyzer.ContainsKey(itemPath) && LatestAnalyzer[itemPath] == predictor && orgFrames[lowIndex][0].Frame.FrameIndex < LatestFrameAnalyzed[itemPath])
                    {
                        lowIndex = -1;
                        break;
                    }
                    TestAndAdd(orgFrames[lowIndex], predictor, itemPath, similarityThreshold);
                    if (currentSize != itemPath.FramedItems.Count || orgFrames[lowIndex].Count == 0)
                    {
                        orgFrames.RemoveAt(lowIndex);
                        if (lowIndex > 0)
                        {
                            --lowIndex;
                        }
                        if (highIndex > 0)
                        {
                            --highIndex;
                        }
                        if (startingIndex > 0)
                        {
                            --startingIndex;
                        }
                        currentSize = itemPath.FramedItems.Count;
                        break;
                    }
                }
                for (; highIndex < orgFrames.Count && orgFrames.Count > 0 && orgFrames.Count > 0; ++highIndex)
                {
                    TestAndAdd(orgFrames[highIndex], predictor, itemPath, similarityThreshold);
                    if (orgFrames.Count != itemPath.FramedItems.Count || orgFrames[highIndex].Count == 0)
                    {
                        orgFrames.RemoveAt(highIndex);
                        break;
                    }
                }
            }
        }

        private void InductTriggeredFrame(IItemPath itemPath, IList<IList<IFramedItem>> orgFrames)
        {
            IItemID triggeredItemID = null;

            var lastFrame = orgFrames.Last();
            for (int i = lastFrame.Count - 1; i >= 0; --i)
            {
                var framedItem = lastFrame[i];
                for (int j = framedItem.ItemIDs.Count - 1; j >= 0; --j)
                {
                    var itemID = framedItem.ItemIDs[j];
                    if (itemID.FurtherAnalysisTriggered)
                    {
                        triggeredItemID = itemID;
                        break;
                    }
                }
                if (triggeredItemID != null)
                {
                    break;
                }
            }
            /**/
            if (triggeredItemID != null)
            {
                var secondLastFrame = orgFrames[orgFrames.Count - 1];
                IFramedItem closestItem = null;
                double maxSim = -999999999;
                for (int i = 0; i < secondLastFrame.Count; i++)
                {
                    var framedItem = secondLastFrame[i];
                    double sim = framedItem.Similarity(triggeredItemID.BoundingBox);
                    if (sim > maxSim)
                    {
                        maxSim = sim;
                        closestItem = framedItem;
                    }
                }

                if (closestItem != null)
                {
                    itemPath.FramedItems.Add(closestItem);
                    orgFrames.RemoveAt(orgFrames.Count - 2);
                }
            }
        }
    }
}
