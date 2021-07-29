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
    public class TriggerIDFilter : Filter
    {
        public TriggerIDFilter()
        {
        }

        public override IDictionary<IFramedItem, bool> FilterCache(IList<IList<IFramedItem>> cache, IItemPath path)
        {
            var filterTable = new Dictionary<IFramedItem, bool>();

            for (int i = 0; i < cache.Count; i++)
            {
                var list = cache[i];
                for (int j = 0; j < list.Count; j++)
                {
                    var fi = list[j];
                    if (fi.ItemIDs.Count == 0)
                    {
                        filterTable.Add(fi, false);
                        continue;
                    }
                    bool filter = true;
                    for (int k = 0; k < fi.ItemIDs.Count; k++)
                    {
                        var id = fi.ItemIDs[k];
                        if(id.FurtherAnalysisTriggered && id is LineTriggeredItemID ltid && ltid.BoundingBox == ltid.TriggerSegment.BoundingBox)
                        {
                            continue;
                        }
                        else
                        {
                            filter = false;
                            break;
                        }
                    }
                    if (!filterTable.ContainsKey(fi))
                    {
                        filterTable.Add(fi, !filter);
                    }
                }
            }

            return filterTable;
        }
    }
}
