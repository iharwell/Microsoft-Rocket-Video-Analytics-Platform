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
    public class SegmentedPathBuilder : IPathBuilder
    {
        public int SegmentLength { get; set; }

        public void BuildPaths(out IDictionary<IFramedItem, IItemPath> pathTable, out IList<IItemPath> paths, IList<IList<IFramedItem>> sortedFramedItems, float iouThreshold)
        {
            pathTable = new Dictionary<IFramedItem,IItemPath>();
            paths = new List<IItemPath>();

            for (int i = 0; i < sortedFramedItems.Count - 1; i += SegmentLength)
            {
                for (int j = 0; j < sortedFramedItems[i].Count; j++)
                {
                    for (int k = i + 1; k < sortedFramedItems.Count && k - i < SegmentLength; ++k)
                    {
                        (var iou, var match) = Motion.FindBestMatch(sortedFramedItems[i][j], sortedFramedItems[i + 1]);
                        if (iou > iouThreshold)
                        {
                            AddToPath(sortedFramedItems[i][j], match, pathTable, paths);
                        }
                    }
                }
            }
        }

        private void AddToPath(IFramedItem framedItem, IFramedItem match, IDictionary<IFramedItem, IItemPath> pathTable, IList<IItemPath> paths)
        {
            if (pathTable.ContainsKey(framedItem))
            {
                if (pathTable.ContainsKey(match))
                {
                    var srcList = pathTable[match];
                    var dstList = pathTable[framedItem];
                    if (dstList != srcList)
                    {
                        dstList.FramedItems.Add(match);
                        for (int i = 0; i < srcList.FramedItems.Count; i++)
                        {
                            var item = srcList.FramedItems[i];
                            pathTable[item] = dstList;
                            if (item == match)
                            {
                                continue;
                            }
                            dstList.FramedItems.Add(item);
                        }
                        paths.Remove(srcList);
                    }
                }
                else
                {
                    pathTable[framedItem].FramedItems.Add(match);
                    pathTable.Add(match, pathTable[framedItem]);
                }
            }
            else
            {
                if (pathTable.ContainsKey(match))
                {
                    pathTable[match].FramedItems.Add(framedItem);
                    pathTable.Add(framedItem, pathTable[match]);
                }
                else
                {
                    var list = new ItemPath();
                    list.FramedItems.Add(framedItem);
                    list.FramedItems.Add(match);
                    pathTable.Add(framedItem, list);
                    pathTable.Add(match, list);
                    paths.Add(list);
                }
            }
        }
    }
}
