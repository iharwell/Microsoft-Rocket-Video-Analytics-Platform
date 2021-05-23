using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils
{
    /// <summary>
    /// A simple base class for <see cref="IItemPath"/> implementations providing default functionality.
    /// </summary>
    public class ItemPath : IItemPath
    {
        protected ItemPath()
        {
            FrameIndices = new List<int>();
            FramedItems = new List<IFramedItem>();
        }

        public IList<int> FrameIndices { get; protected set; }
        public IList<IFramedItem> FramedItems { get; protected set; }


        IList<int> IItemPath.FrameIndices => FrameIndices;

        IList<IFramedItem> IItemPath.FramedItems => FramedItems;
    }
}
