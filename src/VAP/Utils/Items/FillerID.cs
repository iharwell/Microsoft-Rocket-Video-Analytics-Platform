// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Utils.Items
{
    /// <summary>
    /// An IItemID used to indicate that an item was found before and after this frame, but not in it.
    /// </summary>
    [DataContract]
    public class FillerID : IItemID
    {
        public string ObjName { get; set; }
        public int ObjectID { get; set; }
        public int TrackID { get; set; }
        public double Confidence { get; set; }
        public Rectangle BoundingBox { get; set; }
        public string IdentificationMethod { get; set; }
        [IgnoreDataMember]
        public object SourceObject { get; set; }
        public bool FurtherAnalysisTriggered => false;
    }
}
