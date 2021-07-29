// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;

namespace MotionTracker
{
    [DataContract]
    public abstract class Filter : ICacheFilter
    {
        public abstract IDictionary<IFramedItem, bool> FilterCache(IList<IList<IFramedItem>> cache, IItemPath path);

        public IList<IList<IFramedItem>> GetFilteredCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            var table = FilterCache(cache, path);

            List<IList<IFramedItem>> duplicatedCache = new();

            for (int i = 0; i < cache.Count; i++)
            {
                var list = new List<IFramedItem>();
                for (int j = 0; j < cache[i].Count; j++)
                {
                    if (table[cache[i][j]])
                    {
                        list.Add(cache[i][j]);
                    }
                }
                duplicatedCache.Add(list);
            }
            return duplicatedCache;
        }
    }
}
