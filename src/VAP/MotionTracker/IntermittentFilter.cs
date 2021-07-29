// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;
using Utils.ShapeTools;

namespace MotionTracker
{
    [DataContract]
    public class IntermittentFilter : Filter
    {
        public IntermittentFilter()
            : this(0.8f, 6, 15, 0.3f)
        { }
        public IntermittentFilter(float iouThreshold, int continuousLengthThreshold, int totalCountThreshold, float frameDensityThreshold)
        {
            IoUThreshold = iouThreshold;
            ContinuousLengthThreshold = continuousLengthThreshold;
            TotalCountThreshold = totalCountThreshold;
            FrameDensityThreshold = frameDensityThreshold;
        }

        /// <summary>
        ///   Gets or sets the number of continuous frames above which a path will pass through this filter.
        /// </summary>
        [DataMember]
        public int ContinuousLengthThreshold { get; set; }

        /// <summary>
        ///   The intersection-over-union threshold used to build the paths used by this filter.
        /// </summary>
        [DataMember]
        public float IoUThreshold { get; set; }

        /// <summary>
        ///   The number of total frames of a given item needed for this filter to consider
        ///   eliminating a path.
        /// </summary>
        [DataMember]
        public int TotalCountThreshold { get; set; }

        /// <summary>
        ///   The ratio between frames in a path and the number of frames spanned by that path below
        ///   which a path will be considered for removal.
        /// </summary>
        [DataMember]
        public float FrameDensityThreshold { get; set; }

        public override IDictionary<IFramedItem, bool> FilterCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
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

            for (int i = 0; i < cache.Count; i++)
            {
                for (int j = 0; j < cache[i].Count; j++)
                {
                    if(filterTable.ContainsKey(cache[i][j]) && !filterTable[cache[i][j]])
                    {
                        continue;
                    }
                    List<IFramedItem> intermittentSet = new();
                    if (IsIntermittentItem(cache[i][j], cache, filterTable, i, intermittentSet))
                    {
                        for (int m = 0; m < intermittentSet.Count; m++)
                        {
                            //filterTable.Add(intermittentSet[m], false);

                            filterTable[intermittentSet[m]] = false;
                        }
                    }
                }
            }
            return filterTable;
        }

        private bool IsIntermittentItem(IFramedItem framedItem, IList<IList<IFramedItem>> cache, Dictionary<IFramedItem, bool> filterTable, int startIndex, List<IFramedItem> intermittentSet)
        {
            RectangleF rect = framedItem.MeanBounds;

            if(intermittentSet.Count<1)
            {
                intermittentSet.Add(framedItem);
            }

            for (int i = startIndex + 1; i < cache.Count; i++)
            {
                var match = Motion.FindBestMatch(framedItem, cache[i],filterTable);
                if (match.iou > IoUThreshold)
                {
                    intermittentSet.Add(match.match);
                }
            }

            int maxContinuousSection = 1;

            float frameContentDensity = intermittentSet.Count * 1.0f / (intermittentSet.Last().Frame.FrameIndex - intermittentSet.First().Frame.FrameIndex);

            if (intermittentSet.Count < TotalCountThreshold || frameContentDensity > FrameDensityThreshold)
            {
                return false;
            }

            var segments = ContinuousSegments(intermittentSet);

            for (int i = 0; i < segments.Count; i++)
            {
                maxContinuousSection = Math.Max(segments[i].End - segments[i].Start + 1, maxContinuousSection);
            }

            /*
            int prevFrame = framedItem.Frame.FrameIndex;
            int contFrames = 1;
            
            for (int i = 1; i < intermittentSet.Count; i++)
            {
                if (prevFrame + 1 == intermittentSet[i].Frame.FrameIndex)
                {
                    ++contFrames;
                    if (contFrames > maxContinuousSection)
                    {
                        maxContinuousSection = contFrames;
                    }
                }
            }*/

            if (maxContinuousSection<ContinuousLengthThreshold)
            {
                return true;
            }
            return false;
        }

        public static IList<(int Start, int End)> ContinuousSegments(IList<IFramedItem> sortedFrames)
        {
            int start = 0;
            int end = 1;
            List<(int Start, int End)> segments = new();

            int prevFrame = sortedFrames.First().Frame.FrameIndex;

            while (start < sortedFrames.Count)
            {
                if(end == sortedFrames.Count)
                {
                    segments.Add((sortedFrames[start].Frame.FrameIndex, sortedFrames[start].Frame.FrameIndex));
                    break;
                }
                int currentFrame = sortedFrames[end].Frame.FrameIndex;
                while (end < sortedFrames.Count && currentFrame == prevFrame + 1)
                {
                    prevFrame = currentFrame;
                    end++;
                }
                segments.Add((sortedFrames[start].Frame.FrameIndex, sortedFrames[end - 1].Frame.FrameIndex));
                start = end;
                end++;
            }

            return segments;
        }
    }
}
