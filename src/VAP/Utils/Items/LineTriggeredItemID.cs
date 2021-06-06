// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    ///   Default implementation of the <see cref="ILineTriggeredItemID" /> interface.
    /// </summary>

    [DataContract]
    public class LineTriggeredItemID : ItemID, ILineTriggeredItemID
    {
        public LineTriggeredItemID()
        { }

        public LineTriggeredItemID(Rectangle boundingBox, int objectID, string objName, double confidence, int trackID, string identificationMethod)
            : base(boundingBox, objectID, objName, confidence, trackID, identificationMethod)
        {
            FurtherAnalysisTriggered = false;
        }

        /// <inheritdoc />
        [DataMember]
        public string TriggerLine { get; set; }

        /// <inheritdoc />
        [DataMember]
        public int TriggerLineID { get; set; }

        /// <inheritdoc />
        [DataMember]
        public LineSegment TriggerSegment { get; set; }

        [DataMember]
        public bool FurtherAnalysisTriggered { get; set; }
    }
}
