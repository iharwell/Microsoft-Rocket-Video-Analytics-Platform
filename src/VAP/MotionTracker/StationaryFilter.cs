// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;
using System.Runtime.Intrinsics.X86;
using Utils.ShapeTools;
using System.Runtime.Serialization;

namespace MotionTracker
{
    [DataContract]
    public class StationaryFilter : ICacheFilter
    {
        [DataMember]
        private SegmentedPathBuilder _pathBuilder;

        public StationaryFilter()
        {
            _pathBuilder = new SegmentedPathBuilder();
            SavedItems = new List<DurableFilter>();
            VelocityThreshold = 3;
            ConfirmationCount = 5;
            OverlapThreshold = 0.6f;
            ChunkSize = 40;
        }

        [DataMember]
        public int ChunkSize
        {
            get => _pathBuilder.SegmentLength;
            set => _pathBuilder.SegmentLength = value;
        }

        [DataMember]
        public int ConfirmationCount { get; set; }

        [DataMember]
        public float OverlapThreshold { get; set; }

        public float VelocitySaveThreshold => VelocityThreshold / 2;

        /// <summary>
        /// The cutoff velocity below which an object is considered stationary, in pixels per frame.
        /// </summary>
        [DataMember]
        public float VelocityThreshold { get; set; }

        private IList<DurableFilter> SavedItems { get; set; }

        public IDictionary<IFramedItem, bool> FilterCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            IDictionary<IFramedItem, IItemPath> pathTable;
            IList<IItemPath> paths;

            Dictionary<IFramedItem, bool> filterTable = new();

            if (SavedItems.Count > 0)
            {
                for (int i = 0; i < cache.Count; i++)
                {
                    for (int j = 0; j < cache[i].Count; j++)
                    {
                        for (int k = 0; k < SavedItems.Count; k++)
                        {
                            if (!filterTable.ContainsKey(cache[i][j]) && cache[i][j].MeanBounds.IntersectionOverUnion(SavedItems[k].Rectangle) > OverlapThreshold)
                            {
                                filterTable.Add(cache[i][j], false);
                            }
                        }
                    }
                }
            }
            _pathBuilder.BuildPaths(out pathTable, out paths, cache, OverlapThreshold);

            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].FramedItems.Count > ConfirmationCount)
                {
                    float velocity = Motion.PathVelocity(paths[i]);
                    if (velocity < VelocitySaveThreshold)
                    {
                        CheckAndSave(paths[i]);
                    }
                    if (velocity < VelocityThreshold)
                    {
                        FilterPath(paths[i], filterTable);
                    }
                }
            }

            CleanSaves();

            return filterTable;
        }

        public IList<IList<IFramedItem>> GetFilteredCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            IList<IList<IFramedItem>> duplicatedCache = new List<IList<IFramedItem>>();
            for (int i = 0; i < cache.Count; i++)
            {
                duplicatedCache.Add(new List<IFramedItem>(cache[i]));
            }

            Dictionary<IFramedItem, int> itemLocationTable = new();
            IDictionary<IFramedItem, IItemPath> pathTable;
            IList<IItemPath> paths;

            _pathBuilder.BuildPaths(out pathTable, out paths, duplicatedCache, OverlapThreshold);

            for (int i = 0; i < duplicatedCache.Count; i++)
            {
                for (int j = 0; j < duplicatedCache[i].Count; j++)
                {
                    itemLocationTable.Add(duplicatedCache[i][j], duplicatedCache.Count - 1);
                }
            }

            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].FramedItems.Count > ConfirmationCount)
                {
                    float velocity = Motion.PathVelocity(paths[i]);
                    if (velocity < VelocitySaveThreshold)
                    {
                        SavedItems.Add(new DurableFilter(StationaryRectangle(paths[i])));
                    }
                    if (velocity < VelocityThreshold)
                    {
                        RemovePath(paths[i], duplicatedCache, itemLocationTable);
                    }
                }
            }
            return duplicatedCache;
        }

        private void CheckAndSave(IItemPath itemPath)
        {
            var r = StationaryRectangle(itemPath);
            for (int i = 0; i < SavedItems.Count; i++)
            {
                if (r.IntersectionOverUnion(SavedItems[i].Rectangle) > 0.875f)
                {
                    SavedItems[i].StationaryPathCount++;
                    return;
                }
            }

            SavedItems.Add(new DurableFilter(r));
        }

        private void CleanSaves()
        {
            for (int i = 0; i < SavedItems.Count; i++)
            {
                while (i < SavedItems.Count && (SavedItems[i].StationaryPathCount < ConfirmationCount))
                {
                    SavedItems.RemoveAt(i);
                }
            }
            for (int i = 0; i < SavedItems.Count; i++)
            {
                SavedItems[i].StationaryPathCount--;
            }
        }

        private void FilterPath(IItemPath pathToFilter, Dictionary<IFramedItem, bool> filterTable)
        {
            for (int i = 0; i < pathToFilter.FramedItems.Count; i++)
            {
                if (filterTable.ContainsKey(pathToFilter.FramedItems[i]))
                {
                    filterTable[pathToFilter.FramedItems[i]] = false;
                }
                else
                {
                    filterTable.Add(pathToFilter.FramedItems[i], false);
                }
            }
        }

        private void RemovePath(IItemPath pathToRemove, IList<IList<IFramedItem>> duplicatedCache, IDictionary<IFramedItem, int> itemLocationTable)
        {
            for (int i = 0; i < pathToRemove.FramedItems.Count; i++)
            {
                var item = pathToRemove.FramedItems[i];
                duplicatedCache[itemLocationTable[item]].Remove(item);
            }
        }

        private unsafe RectangleF StationaryRectangle(IItemPath itemPath)
        {
            RectangleF rect = itemPath.FramedItems[0].MeanBounds;
            RectangleF rectEnd = itemPath.FramedItems[^1].MeanBounds;
            rect = new RectangleF(0.5f * (rect.X + rectEnd.X), 0.5f * (rect.Y + rectEnd.Y), 0.5f * (rect.Width + rectEnd.Width), 0.5f * (rect.Height + rectEnd.Height));

            Vector128<float> avg = new Vector128<float>();
            var tmpVec = Sse.LoadVector128((float*)&rect);
            avg = Sse.Add(tmpVec, tmpVec);
            for (int i = 1; i < itemPath.FramedItems.Count - 1; i++)
            {
                var mean = itemPath.FramedItems[i].MeanBounds;
                if (mean.IntersectionOverUnion(rect) > 0.875)
                {
                    tmpVec = Sse.LoadVector128((float*)&mean);
                    avg = Sse.Add(avg, tmpVec);
                }
            }
            float div = 1.0f / itemPath.FramedItems.Count;

            tmpVec = Sse.LoadScalarVector128(&div);
            avg = Sse.Multiply(avg, tmpVec);
            Sse.StoreScalar((float*)&rect, avg);
            return rect;
        }

        private class DurableFilter
        {
            public DurableFilter(RectangleF rect)
            {
                Rectangle = rect;
            }
            public RectangleF Rectangle { get; set; }
            public int StationaryPathCount { get; set; }
        }
    }
}
