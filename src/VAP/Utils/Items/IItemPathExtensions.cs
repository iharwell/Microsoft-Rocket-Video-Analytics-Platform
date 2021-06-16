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
    }
}
