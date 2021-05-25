using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    ///   Default implementation of the <see cref="ILineTriggeredItemID" /> interface.
    /// </summary>
    public class LineTriggeredItemID : ItemID, ILineTriggeredItemID
    {
        public LineTriggeredItemID()
        { }

        public LineTriggeredItemID( Rectangle boundingBox, int objectID, string objName, double confidence, int trackID, string identificationMethod )
            : base( boundingBox, objectID, objName, confidence, trackID, identificationMethod)
        { }

        /// <inheritdoc />
        public string TriggerLine { get; set; }

        /// <inheritdoc />
        public int TriggerLineID { get; set; }
    }
}
