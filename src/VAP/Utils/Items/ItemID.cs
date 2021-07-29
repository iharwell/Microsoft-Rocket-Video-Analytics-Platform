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
    ///   Default implementation of the <see cref="IItemID" /> interface.
    /// </summary>

    [Serializable]
    [DataContract]
    public class ItemID : IItemID, IDisposable
    {
        private bool _disposedValue;

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
        public ItemID(IItemID id)
            : this(id.BoundingBox, id.ObjectID, id.ObjName, id.Confidence, id.TrackID, id.IdentificationMethod)
        {
            SourceObject = id.SourceObject;
            FurtherAnalysisTriggered = id.FurtherAnalysisTriggered;
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
        protected ItemID(SerializationInfo info, StreamingContext context)
        {
            ObjName = info.GetString(nameof(ObjName));
            ObjectID = info.GetInt32(nameof(ObjectID));
            TrackID = info.GetInt32(nameof(TrackID));
            Confidence = info.GetDouble(nameof(Confidence));
            int x = info.GetInt32(nameof(BoundingBox.X));
            int y = info.GetInt32(nameof(BoundingBox.Y));
            int w = info.GetInt32(nameof(BoundingBox.Width));
            int h = info.GetInt32(nameof(BoundingBox.Height));
            BoundingBox = new Rectangle(x, y, w, h);
            IdentificationMethod = info.GetString(nameof(IdentificationMethod));
        }

        /// <inheritdoc />
        [DataMember]
        public string ObjName { get; set; }

        /// <inheritdoc />
        [DataMember]
        public int ObjectID { get; set; }

        /// <inheritdoc />
        [DataMember]
        public int TrackID { get; set; }

        /// <inheritdoc />
        [DataMember]
        public double Confidence { get; set; }

        /// <inheritdoc />
        [DataMember]
        public Rectangle BoundingBox { get; set; }

        /// <inheritdoc />
        [DataMember]
        public string IdentificationMethod { get; set; }

        /// <inheritdoc />
        [DataMember]
        public bool FurtherAnalysisTriggered { get; set; }

        /// <inheritdoc />
        public object SourceObject { get; set; }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ObjName), ObjName);
            info.AddValue(nameof(ObjectID), ObjectID);
            info.AddValue(nameof(TrackID), TrackID);
            info.AddValue(nameof(Confidence), Confidence);
            info.AddValue(nameof(BoundingBox.X), BoundingBox.X);
            info.AddValue(nameof(BoundingBox.Y), BoundingBox.Y);
            info.AddValue(nameof(BoundingBox.Width), BoundingBox.Width);
            info.AddValue(nameof(BoundingBox.Height), BoundingBox.Height);
            info.AddValue(nameof(IdentificationMethod), IdentificationMethod);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    this.SourceObject = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ItemID()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
