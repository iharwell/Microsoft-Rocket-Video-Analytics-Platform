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

        public static IEnumerable<IFramedItem> NearestItems(this IItemPath path, int frameIndex)
        {
            var nearestItems = from item in path.FramedItems
                               where !(item.ItemIDs.Count == 1 && item.ItemIDs[0] is FillerID)
                               orderby Math.Abs(frameIndex - item.Frame.FrameIndex) ascending
                               select item;
            return nearestItems;
        }
    }
}
