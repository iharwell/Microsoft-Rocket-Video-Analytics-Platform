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
    [KnownType(typeof(HashSet<string>))]
    public class CategoryFilter : ICacheFilter
    {
        public CategoryFilter()
        {
            ExcludeCategories = new HashSet<string>();
        }

        [DataMember]
        public HashSet<string> ExcludeCategories { get; set; }

        public IDictionary<IFramedItem, bool> FilterCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            IDictionary<IFramedItem, bool> results = new Dictionary<IFramedItem, bool>();
            for (int i = 0; i < cache.Count; i++)
            {
                for (int j = 0; j < cache[i].Count; j++)
                {
                    for (int k = 0; k < cache[i][j].ItemIDs.Count; k++)
                    {
                        if(ExcludeCategories.Contains(cache[i][j].ItemIDs[k].ObjName))
                        {
                            results.Add(cache[i][j], false);
                            break;
                        }
                    }
                    if(!results.ContainsKey(cache[i][j]))
                    {
                        results.Add(cache[i][j], true);
                    }
                }
            }

            return results;
        }

        public IList<IList<IFramedItem>> GetFilteredCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            List<IList<IFramedItem>> copiedCache = new();

            var filter = FilterCache(cache, path);

            for (int i = 0; i < cache.Count; i++)
            {
                copiedCache.Add(new List<IFramedItem>());
                for (int j = 0; j < cache[i].Count; j++)
                {
                    if (filter[cache[i][j]])
                    {
                        copiedCache[i].Add(cache[i][j]);
                    }
                }
            }

            return copiedCache;
        }
    }
}
