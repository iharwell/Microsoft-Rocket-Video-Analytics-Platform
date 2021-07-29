// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using OpenCvSharp;
using Utils.Items;
using Utils.ShapeTools;

namespace MotionTracker
{
    /// <summary>
    /// Provides motion tracking support for <see cref="IFramedItem"/> sets.
    /// </summary>
    [DataContract]
    [KnownType(typeof(List<ICacheFilter>))]
    [KnownType(typeof(List<(IPathPredictor module, float threshold)>))]
    [KnownType(typeof(CategoryFilter))]
    [KnownType(typeof(CollisionFilter))]
    [KnownType(typeof(IntermittentFilter))]
    [KnownType(typeof(StationaryFilter))]
    [KnownType(typeof(TriggerIDFilter))]
    [KnownType(typeof(IoUPredictor))]
    [KnownType(typeof(PiecewisePredictor))]
    [KnownType(typeof(CenterPolyPredictor))]
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
            /*LatestFrameAnalyzed = new();
            LatestAnalyzer = new();*/
        }

        [DataMember]
        public IList<ICacheFilter> Filters { get; protected set; }

        [DataMember]
        public int PostPathPad { get; set; }

        [DataMember]
        public IList<(IPathPredictor module, float threshold)> Predictors { get; protected set; }
        /*private Dictionary<IItemPath, IPathPredictor> LatestAnalyzer { get; set; }
        private Dictionary<IItemPath, int> LatestFrameAnalyzed { get; set; }*/

        [DataMember]
        public int PrePathPad { get; set; }
        [DataMember]
        public IList<float> ThresholdCoefProgression { get; set; }

        [DataMember]
        public bool DisplayProcess { get; set; }

        [DataMember]
        public bool ManuallyStepFrames { get; set; }

        public IItemPath BuildPath(IFramedItem framedID, IList<IList<IFramedItem>> buffer, bool suppressOutput)
        {
            IItemPath itemPath = new ItemPath();
            itemPath.FramedItems.Add(framedID);

            if (!SetupBuilder(framedID,
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

            if (filterTable.ContainsKey(framedID) && !filterTable[framedID])
            {
                return null;
            }

            if (orgFrames.Count == 0)
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
                //ScrubPath(itemPath);
                matchFound = startCount != endCount;
            } while (matchFound && orgFrames.Count > 0)
                ;
            /*
            List<IItemPath> pathsToRemove = new();
            foreach (var entry in LatestFrameAnalyzed)
            {
                if(maxFrame - entry.Key.FramedItems.Last().Frame.FrameIndex > 500)
                {
                }
            }*/

            /*if (orgFrames.Count > 0)
            {
                int minFrame = orgFrames[0][0].Frame.FrameIndex;

                int earliestPathIndex = (from item in itemPath.FramedItems
                                         orderby item.Frame.FrameIndex ascending
                                         select item.Frame.FrameIndex).First();

                if (earliestCachedFrame < earliestPathIndex)
                {
                    int startPoint = earliestPathIndex - earliestCachedFrame;
                    for (int i = startPoint - 1; i >= 0 && startPoint - i <= PrePathPad; --i)
                    {
                        FillerID ID = new();
                        var fillerFrame = new FramedItem(orgFrames[i][0].Frame, ID);
                        itemPath.FramedItems.Add(fillerFrame);
                    }
                }
            }*/

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

            Motion.RemoveUsedFrames(itemPath, orgFrames, ref startingIndex);

            if (orgFrames.Count == 0)
            {
                /*LatestAnalyzer[itemPath] = predictor;
                LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;*/
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

            /*LatestAnalyzer[itemPath] = predictor;
            LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;*/
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

            orgFrames = Motion.EnsurePathPresenceInCache(framedID, givenIndex, orgFrames);
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

            Motion.RemoveUsedFrames(itemPath, orgFrames, ref startingIndex);
            if (orgFrames.Count == 0)
            {
                return itemPath;
            }

            InductionPass(predictor, similarityThreshold, itemPath, orgFrames, ref startingIndex);

            /*LatestAnalyzer[itemPath] = predictor;
            LatestFrameAnalyzed[itemPath] = buffer.Last()[0].Frame.FrameIndex;*/
            return itemPath;
        }

        public void ScrubPath(IItemPath path)
        {
            /*var fItems = path.FramedItems;
            for (int i = 45; i < fItems.Count; i++)
            {
                var item = fItems[i];
                int hitCount = 0;

                fItems.RemoveAt(i);

                for (int j = 0; j < Predictors.Count; j++)
                {
                    var rect = Predictors[j].module.Predict(path, item.Frame.FrameIndex);
                    if(rect.IntersectionOverUnion(item.MeanBounds) > 0)
                    {
                        hitCount++;
                    }
                }

                if (hitCount == 0)
                {
                    --i;
                }
                else
                {
                    fItems.Insert(i, item);
                }
            }*/
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
                            var tgtFi = Motion.GetFramedItemByFrameNumber(tgtPath, k);
                            var srcFi = Motion.GetFramedItemByFrameNumber(path, k);

                            if (tgtFi == null || srcFi == null)
                            {
                                matchCount += PredictiveMatchCount(path, tgtPath, k, similarityThreshold);
                            }

                            if (matchCount > 5 || Motion.AreFramedItemsMatched(srcFi, tgtFi, similarityThreshold))
                            {
                                Motion.MergePaths(boundaries, path, ref minFrame, ref maxFrame, tgtPath, tgtMin, tgtMax);
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
                        if (!results.ContainsKey(entry.Key))
                        {
                            results.Add(entry.Key, entry.Value);
                        }
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
            /*if (LatestAnalyzer.ContainsKey(itemPath) && LatestAnalyzer[itemPath] == predictor && orgFrames[highIndex][0].Frame.FrameIndex > LatestFrameAnalyzed[itemPath])
            {
                highIndex = orgFrames[highIndex][0].Frame.FrameIndex - orgFrames[0][0].Frame.FrameIndex;
            }*/
            while ((lowIndex >= 0 || highIndex < orgFrames.Count) && orgFrames.Count > 0)
            {
                int currentSize = itemPath.FramedItems.Count;
                for (; lowIndex >= 0 && currentSize > 0 && orgFrames.Count > 0; --lowIndex)
                {
                    /*if (LatestAnalyzer.ContainsKey(itemPath) && LatestAnalyzer[itemPath] == predictor && orgFrames[lowIndex][0].Frame.FrameIndex < LatestFrameAnalyzed[itemPath])
                    {
                        lowIndex = -1;
                        break;
                    }*/
                    if (predictor.CanPredict(itemPath, orgFrames[lowIndex][0].Frame.FrameIndex))
                    {
                        Motion.TestAndAdd(orgFrames[lowIndex], predictor, itemPath, similarityThreshold, DisplayProcess, ManuallyStepFrames);
                    }
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
                    if (predictor.CanPredict(itemPath, orgFrames[highIndex][0].Frame.FrameIndex))
                    {
                        Motion.TestAndAdd(orgFrames[highIndex], predictor, itemPath, similarityThreshold, DisplayProcess, ManuallyStepFrames);
                    }
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

        private int PredictiveMatchCount(IItemPath path1, IItemPath path2, int frameIndex, float similarityThreshold)
        {
            var fi1 = path1.GetFramedItemByFrameNumber(frameIndex);
            var fi2 = path2.GetFramedItemByFrameNumber(frameIndex);
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
                /*LatestAnalyzer[itemPath] = predictor;
                LatestFrameAnalyzed[itemPath] = latestFrame;*/
                return true;
            }
            return false;
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

            orgFrames = Motion.EnsurePathPresenceInCache(framedID, givenIndex, orgFrames);
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
            Motion.RemoveUsedFrames(itemPath, orgFrames, ref startingIndex);
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
            Motion.RemoveUsedFrames(itemPath, orgFrames, ref startingIndex);
            counts = new List<int>();
        }

        private float VelocityBetween(IFramedItem item1, IFramedItem item2)
        {
            PointF p1 = item1.MeanBounds.Center();
            PointF p2 = item2.MeanBounds.Center();

            PointF delta = new PointF(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));

            return MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y) / Math.Abs(item1.Frame.FrameIndex - item2.Frame.FrameIndex);
        }
    }
}
