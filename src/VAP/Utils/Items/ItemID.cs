using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    ///   Default implementation of the <see cref="IItemID" /> interface.
    /// </summary>
    public class ItemID : IItemID
    {
        /// <summary>
        ///   Creates an <see cref="ItemID" /> object with no information on categorization.
        /// </summary>
        public ItemID()
        {
            ObjName = null;
            ObjectID = -1;
            TrackID = -1;
            Confidence = 0.0;
        }

        public ItemID(Rectangle boundingBox, int objectID, string objName, double confidence, int trackID, string identificationMethod)
        {
            ObjName = objName;
            ObjectID = objectID;
            TrackID = trackID;
            Confidence = confidence;
            BoundingBox = boundingBox;
            IdentificationMethod = identificationMethod;
        }

        /// <inheritdoc />
        public string ObjName { get; set; }

        /// <inheritdoc />
        public int ObjectID { get; set; }

        /// <inheritdoc />
        public int TrackID { get; set; }

        /// <inheritdoc />
        public double Confidence { get; set; }

        /// <inheritdoc />
        public Rectangle BoundingBox { get; set; }

        /// <inheritdoc />
        public string IdentificationMethod { get; set; }
    }
}
