using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    public class LineTriggeredItemID : ItemID, ILineTriggeredItemID
    {
        public LineTriggeredItemID()
        {

        }

        public LineTriggeredItemID( Rectangle boundingBox, int objectID, string objName, double confidence, int trackID, string identificationMethod )
            : base( boundingBox, objectID, objName, confidence, trackID, identificationMethod)
        {

        }
        public string TriggerLine { get; set; }
        public int TriggerLineID { get; set; }
    }
}
