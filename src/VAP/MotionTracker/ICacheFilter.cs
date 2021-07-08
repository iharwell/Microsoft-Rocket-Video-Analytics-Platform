// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Utils.Items;

namespace MotionTracker
{
    public interface ICacheFilter
    {
        IDictionary<IFramedItem, bool> FilterCache(IList<IList<IFramedItem>> cache, IItemPath path);
        IList<IList<IFramedItem>> GetFilteredCache(IList<IList<IFramedItem>> cache, IItemPath path);
    }
}
