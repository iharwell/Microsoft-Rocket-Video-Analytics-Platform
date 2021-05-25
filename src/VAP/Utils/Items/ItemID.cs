using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    public class ItemID : IItemID
    {
        public ItemID()
        { }

        public ItemID( Rectangle boundingBox, int objectID, string objName, double confidence, int trackID, string identificationMethod )
        {
            ObjName = objName;
            ObjectID = objectID;
            TrackID = trackID;
            Confidence = confidence;
            BoundingBox = boundingBox;
            IdentificationMethod = identificationMethod;
        }

        public string ObjName { get; set; }
        public int ObjectID { get; set; }
        public int TrackID { get; set; }
        public double Confidence { get; set; }
        public Rectangle BoundingBox { get; set; }
        public string IdentificationMethod { get; set; }
    }
}
