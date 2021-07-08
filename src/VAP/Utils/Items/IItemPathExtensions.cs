// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.Items
{
    public static class IItemPathExtensions
    {
        public static (int minFrame, int maxFrame) GetPathBounds(this IItemPath path)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                var frame = path.FramedItems[i].Frame;
                min = Math.Min(min, frame.FrameIndex);
                max = Math.Max(max, frame.FrameIndex);
            }

            return (min, max);
        }

        public static int IndexOfNearestFrame(this IItemPath path, int frameIndex)
        {
            int nearestIndex = -1;
            int nearestOffset = 999999999;

            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                if (Math.Abs(frameIndex - path.FrameIndex(i)) < nearestOffset)
                {
                    nearestIndex = i;
                    nearestOffset = Math.Abs(frameIndex - path.FrameIndex(i));
                }
            }

            return nearestIndex;
        }

        public static IList<IFramedItem> NearestItems(this IItemPath path, int frameIndex, int count)
        {
            /*var nearestItems = from item in path.FramedItems
                               where !(item.ItemIDs.Count == 1 && item.ItemIDs[0] is FillerID)
                               orderby Math.Abs(frameIndex - item.Frame.FrameIndex) ascending
                               select item;*/
            var sortedItems = SortedItems(path);

            int index = IndexOfFrameIndex(sortedItems, frameIndex);
            List<IFramedItem> nearestItems = new List<IFramedItem>();
            nearestItems.Add(sortedItems[index]);
            int lowIndex = index - 1;
            int highIndex = index + 1;
            if (lowIndex > 0 && lowIndex < sortedItems.Count
                && highIndex > 0 && highIndex < sortedItems.Count)
            {
                while (nearestItems.Count < count
                    && lowIndex >= 0 && highIndex < sortedItems.Count)
                {
                    while (lowIndex >= 0
                        && nearestItems.Count < count
                        && ((!IsIndexInRange(highIndex, sortedItems) && IsIndexInRange(lowIndex, sortedItems))
                            || Math.Abs(sortedItems[highIndex].Frame.FrameIndex - frameIndex) >= Math.Abs(sortedItems[lowIndex].Frame.FrameIndex - frameIndex)))
                    {
                        nearestItems.Add(sortedItems[lowIndex]);
                        --lowIndex;
                    }
                    while (highIndex < sortedItems.Count
                        && nearestItems.Count < count
                        && ((IsIndexInRange(highIndex, sortedItems) && !IsIndexInRange(lowIndex, sortedItems))
                            || Math.Abs(sortedItems[highIndex].Frame.FrameIndex - frameIndex) <= Math.Abs(sortedItems[lowIndex].Frame.FrameIndex - frameIndex)))
                    {
                        nearestItems.Add(sortedItems[highIndex]);
                        ++highIndex;
                    }
                }
            }
            else if (lowIndex > 0 && lowIndex < sortedItems.Count)
            {
                while (nearestItems.Count < count
                    && lowIndex >= 0)
                {
                    nearestItems.Add(sortedItems[lowIndex]);
                }
            }
            else
            {
                while (highIndex < sortedItems.Count
                    && nearestItems.Count < count)
                {
                    nearestItems.Add(sortedItems[highIndex]);
                    ++highIndex;
                }
            }
            return nearestItems;
        }
        private static bool IsIndexInRange<T>(int index, IList<T> list)
        {
            return index >= 0 && index < list.Count;
        }

        private static int IndexOfFrameIndex( List<IFramedItem> sortedItems, int frameIndex)
        {
            int upperIndex = sortedItems.Count - 1;
            int lowerIndex = 0;

            if(sortedItems[lowerIndex].Frame.FrameIndex >= frameIndex)
            {
                return lowerIndex;
            }
            if (sortedItems[upperIndex].Frame.FrameIndex <= frameIndex)
            {
                return upperIndex;
            }

            while (upperIndex != lowerIndex)
            {
                int mid = (upperIndex + lowerIndex) >> 1;

                int comp = sortedItems[mid].Frame.FrameIndex.CompareTo(frameIndex);
                if (comp == 0)
                {
                    return mid;
                }
                else if (0 < comp)
                {
                    upperIndex = mid;
                }
                else if (comp < 0)
                {
                    if (mid == lowerIndex)
                    {
                        lowerIndex = mid + 1;
                    }
                    else
                    {
                        lowerIndex = mid;
                    }
                }
            }
            return lowerIndex;
        }
        public static List<IFramedItem> SortedItems(this IItemPath path)
        {
            /*var nearestItems = from item in path.FramedItems
                               where !(item.ItemIDs.Count == 1 && item.ItemIDs[0] is FillerID)
                               orderby Math.Abs(frameIndex - item.Frame.FrameIndex) ascending
                               select item;*/
            List<IFramedItem> items = new List<IFramedItem>();
            var fi = path.FramedItems;
            for (int i = 0; i < fi.Count; i++)
            {
                var item = fi[i];
                if (item.ItemIDs.Count == 1 && item.ItemIDs[0] is FillerID)
                {
                    continue;
                }
                items.Add(item);
            }
            items.Sort((IFramedItem a, IFramedItem b) => a.Frame.FrameIndex - b.Frame.FrameIndex);
            return items;
        }
    }
}
