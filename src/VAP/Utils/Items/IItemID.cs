using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    /// Interface for the identification info on an item. Specifically, this interface exposes information from DNN identification.
    /// </summary>
    public interface IItemID
    {
        /// <summary>
        /// The item label as determined by the identification method of choice.
        /// </summary>
        /// <remarks>Set to null to indicate that no identification was performed.</remarks>
        string ObjName { get; set; }

        int ObjectID { get; set; }

        int TrackID { get; set; }

        /// <summary>
        /// The confidence of this identification as determined by the method of choice. 0 indicates extreme uncertainty, and 1 indicates extreme certainty.
        /// </summary>
        /// <remarks>Returns a value of 0 if no identification is attempted.</remarks>
        double Confidence { get; set; }

        /// <summary>
        /// The bounding box of this item as determined by the indentification method of choice.
        /// </summary>
        Rectangle BoundingBox { get; set; }

        /// <summary>
        /// The identification method that created this ID.
        /// </summary>
        string IdentificationMethod { get; set; }
    }
}
