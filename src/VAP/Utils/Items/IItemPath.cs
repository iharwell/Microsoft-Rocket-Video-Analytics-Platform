// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    ///   Exposes a path that an item took through some number of frames.
    /// </summary>
    public interface IItemPath
    {
        /// <summary>
        ///   Gives the frame index for the framed item at the given index.
        /// </summary>
        int FrameIndex(int entryIndex);

        /// <summary>
        ///   A list of the <see cref="IFramedItem" /> objects with this item in all frames in which
        ///   it was found.
        /// </summary>
        IList<IFramedItem> FramedItems { get; }

        int HighestConfidenceFrameIndex { get; }
        int HighestConfidenceIDIndex { get; }
    }
}
