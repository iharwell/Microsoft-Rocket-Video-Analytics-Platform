using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    /// An interface exposing an item that was found in a frame.
    /// </summary>
    public interface IFramedItem
    {
        /// <summary>
        /// The frame that the item was found in.
        /// </summary>
        IFrame Frame { get; set; }

        /// <summary>
        /// All <see cref="ItemIDs"/> for the item in this frame.
        /// </summary>
        IList<IItemID> ItemIDs { get; }

        /// <summary>
        /// Retrieves the tagged image data for the item using the <see cref="IItemID"/> found at the given index of <see cref="ItemIDs"/>.
        /// </summary>
        /// <param name="itemIDIndex">The index of the <see cref="IItemID"/> to use.</param>
        /// <returns>Returns a <c>byte[]</c> of the tagged image data for the requested <see cref="IItemID"/> in this frame.</returns>
        byte[] TaggedImageData( int itemIDIndex );

        /// <summary>
        /// Retrieves the cropped image data for the item using the <see cref="IItemID"/> found at the given index of <see cref="ItemIDs"/>.
        /// </summary>
        /// <param name="itemIDIndex">The index of the <see cref="IItemID"/> to use.</param>
        /// <returns>Returns a <c>byte[]</c> of the cropped image data for the requested <see cref="IItemID"/> in this frame.</returns>
        byte[] CroppedImageData( int itemIDIndex );
    }
}
