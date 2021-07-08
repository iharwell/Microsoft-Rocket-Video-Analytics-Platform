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
    public interface IPathBuilder
    {
        public void BuildPaths(out IDictionary<IFramedItem, IItemPath> pathTable, out IList<IItemPath> paths, IList<IList<IFramedItem>> sortedFramedItems, float iouThreshold);
    }
}
