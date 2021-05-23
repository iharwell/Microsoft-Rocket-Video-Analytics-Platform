using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils
{
    /// <summary>
    /// An abstract base class for <see cref="IItemPath"/> implementations providing default functionality.
    /// </summary>
    public abstract class ItemPath : IItemPath
    {
        public IList<int> FrameIndices { get; protected set; }
        public IList<IFramedItem> FramedItems { get; protected set; }


        IList<int> IItemPath.FrameIndices => FrameIndices;

        IList<IFramedItem> IItemPath.FramedItems => FramedItems;
    }
}
