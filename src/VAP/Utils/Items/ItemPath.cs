// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    ///   A simple base class for <see cref="IItemPath" /> implementations providing default functionality.
    /// </summary>
    public class ItemPath : IItemPath
    {
        public ItemPath()
        {
            FramedItems = new List<IFramedItem>();
        }

        /// <inheritdoc />
        public virtual int FrameIndex(int entryIndex)
        {
            return FramedItems[entryIndex].Frame.FrameIndex;
        }

        /// <inheritdoc cref="IItemPath.FramedItems" />
        public IList<IFramedItem> FramedItems { get; protected set; }

        /// <inheritdoc />
        IList<IFramedItem> IItemPath.FramedItems => FramedItems;
    }
}
