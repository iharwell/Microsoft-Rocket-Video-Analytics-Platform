using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils
{

    public interface IItemPath
    {
        /// <summary>
        /// A list of the frame indices that the item was seen in.
        /// </summary>
        IList<int> FrameIndices { get; }

        /// <summary>
        /// A list of the <see cref="IFramedItem"/> objects with this item in all frames in which it was found.
        /// </summary>
        IList<IFramedItem> FramedItems { get; }
    }
}
