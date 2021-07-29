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
    public class CollisionEventArgs : EventArgs
    {
        public CollisionEventArgs(IFramedItem item1, IFramedItem item2, IDictionary<IFramedItem, IItemPath> pathTable, IList<IItemPath> paths)
        {
            Item1 = item1;
            Item2 = item2;
            PathTable = pathTable;
            Paths = paths;
            ContinueMerge = true;
            KeepCurrentPathForItem2 = true;
        }

        public bool ContinueMerge { get; set; }
        public IFramedItem Item1 { get; set; }
        public IFramedItem Item2 { get; set; }
        public bool KeepCurrentPathForItem2 { get; set; }
        public IList<IItemPath> Paths { get; set; }
        public IDictionary<IFramedItem, IItemPath> PathTable { get; set; }
    }

    [DataContract]
    public class SegmentedPathBuilder : IPathBuilder
    {
        public delegate void CollisionCallback(object sender, CollisionEventArgs args);

        public event CollisionCallback OnCollision;

        [DataMember]
        public int SegmentLength { get; set; }

        [DataMember]
        public IPathPredictor Predictor { get; set; }

        public void BuildPaths(out IDictionary<IFramedItem, IItemPath> pathTable, out IList<IItemPath> paths, IList<IList<IFramedItem>> sortedFramedItems, float iouThreshold)
        {
            BuildPaths(out pathTable, out paths, sortedFramedItems, iouThreshold, null);

            /*pathTable = new Dictionary<IFramedItem,IItemPath>();
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
            }*/
        }
        public void BuildPaths(out IDictionary<IFramedItem, IItemPath> pathTable,
                               out IList<IItemPath> paths,
                               IList<IList<IFramedItem>> sortedFramedItems,
                               float iouThreshold,
                               IDictionary<IFramedItem, bool> filterTable)
        {
            pathTable = new Dictionary<IFramedItem, IItemPath>();
            paths = new List<IItemPath>();

            for (int i = 0; i < sortedFramedItems.Count - 1; i += SegmentLength)
            {
                for (int k = i + 1; k < sortedFramedItems.Count && k - i < SegmentLength; ++k)
                {
                    for (int j = 0; j < sortedFramedItems[i].Count; j++)
                    {
                        var item1 = sortedFramedItems[i][j];
                        if (filterTable != null && filterTable.ContainsKey(sortedFramedItems[i][j]) && !filterTable[sortedFramedItems[i][j]])
                        {
                            continue;
                        }
                        IFramedItem match;
                        float iou;
                        if (Predictor == null || !pathTable.ContainsKey(item1))
                        {
                            (iou, match) = Motion.FindBestMatch(sortedFramedItems[i][j], sortedFramedItems[k], filterTable);
                        }
                        else
                        {
                            var path = pathTable[item1];
                            var rectangle = Predictor.Predict(path, sortedFramedItems[k][0].Frame.FrameIndex);
                            (iou, match) = Motion.FindBestMatch(rectangle, sortedFramedItems[k], filterTable);
                        }
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
                    bool continueMerge = true;
                    bool keepMatchPathAsIs = true;
                    if (OnCollision != null)
                    {
                        var args = new CollisionEventArgs(framedItem, match, pathTable, paths);
                        OnCollision(this, args);
                        continueMerge = args.ContinueMerge;
                        keepMatchPathAsIs = args.KeepCurrentPathForItem2;
                    }
                    if (continueMerge)
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
                    else if (!keepMatchPathAsIs)
                    {
                        var srcList = pathTable[match];
                        var dstList = pathTable[framedItem];
                        srcList.FramedItems.Remove(match);
                        dstList.FramedItems.Remove(framedItem);
                        pathTable.Remove(match);
                        pathTable.Remove(framedItem);
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
