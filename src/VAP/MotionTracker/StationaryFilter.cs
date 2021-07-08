// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;
using Utils.ShapeTools;

namespace MotionTracker
{
    public class StationaryFilter : ICacheFilter
    {
        public StationaryFilter()
        {
            _pathBuilder = new SegmentedPathBuilder();
            VelocityThreshold = 3;
            ConfirmationCount = 5;
            OverlapThreshold = 0.4f;
            ChunkSize = 30;
        }

        /// <summary>
        /// The cutoff velocity below which an object is considered stationary, in pixels per frame.
        /// </summary>
        public float VelocityThreshold { get; set; }

        public int ConfirmationCount { get; set; }

        public float OverlapThreshold { get; set; }

        public int ChunkSize
        {
            get => _pathBuilder.SegmentLength;
            set => _pathBuilder.SegmentLength = value;
        }

        private SegmentedPathBuilder _pathBuilder;
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
                    if(velocity < VelocityThreshold)
                    {
                        RemovePath(paths[i], duplicatedCache, itemLocationTable);
                    }
                }
            }
            return duplicatedCache;
        }

        private void RemovePath(IItemPath pathToRemove, IList<IList<IFramedItem>> duplicatedCache, IDictionary<IFramedItem, int> itemLocationTable)
        {
            for (int i = 0; i < pathToRemove.FramedItems.Count; i++)
            {
                var item = pathToRemove.FramedItems[i];
                duplicatedCache[itemLocationTable[item]].Remove(item);
            }
        }

        public IDictionary<IFramedItem, bool> FilterCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            IDictionary<IFramedItem, IItemPath> pathTable;
            IList<IItemPath> paths;

            Dictionary<IFramedItem, bool> filterTable = new();

            _pathBuilder.BuildPaths(out pathTable, out paths, cache, OverlapThreshold);

            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].FramedItems.Count > ConfirmationCount)
                {
                    float velocity = Motion.PathVelocity(paths[i]);
                    if (velocity < VelocityThreshold)
                    {
                        FilterPath(paths[i], filterTable);
                    }
                }
            }
            return filterTable;
        }

        private void FilterPath(IItemPath pathToFilter, Dictionary<IFramedItem, bool> filterTable)
        {
            for (int i = 0; i < pathToFilter.FramedItems.Count; i++)
            {
                if(filterTable.ContainsKey(pathToFilter.FramedItems[i]))
                {
                    filterTable[pathToFilter.FramedItems[i]] = false;
                }
                else
                {
                    filterTable.Add(pathToFilter.FramedItems[i], false);
                }
            }
        }
    }
}
