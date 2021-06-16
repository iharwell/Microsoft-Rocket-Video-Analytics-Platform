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
    ///   Interface for the identification info on an item. Specifically, this interface exposes
    ///   information from DNN identification.
    /// </summary>
    public interface IItemID
    {
        /// <summary>
        ///   The item label as determined by the identification method of choice.
        /// </summary>
        /// <remarks>
        ///   Set to <see langword="null" /> to indicate that no identification was performed.
        /// </remarks>
        string ObjName { get; set; }

        /// <summary>
        ///   Used internally by identification systems. Usually used as an index into some list of
        ///   object categories.
        /// </summary>
        /// <remarks>
        ///   Set to -1 if no identification was performed.
        /// </remarks>
        int ObjectID { get; set; }

        /// <summary>
        ///   Used internally by identification systems. Usually used as an index into the list of
        ///   identified items in a frame.
        /// </summary>
        /// <remarks>
        ///   Set to -1 if no identification was performed.
        /// </remarks>
        int TrackID { get; set; }

        /// <summary>
        ///   The confidence of this identification as determined by the method of choice. 0
        ///   indicates extreme uncertainty, and 1 indicates extreme certainty.
        /// </summary>
        /// <remarks>
        ///   Returns a value of 0 if no identification is attempted.
        /// </remarks>
        double Confidence { get; set; }

        /// <summary>
        ///   The bounding box of this item as determined by the indentification method of choice.
        /// </summary>
        Rectangle BoundingBox { get; set; }

        //TODO(iharwell): possibly switch to an object reference to the object that generated the ID, rather than using the name. This would allow more options using reflection.
        /// <summary>
        ///   The identification method that created this ID.
        /// </summary>
        /// <remarks>
        ///   This should usually be set using <see langword="nameof" /> with the class that
        ///   generated the object.
        /// </remarks>
        string IdentificationMethod { get; set; }

        /// <summary>
        ///   The identification method that created this ID.
        /// </summary>
        /// <remarks>
        ///   This should usually be set using <see langword="nameof" /> with the class that
        ///   generated the object.
        /// </remarks>
        object SourceObject { get; set; }

        bool FurtherAnalysisTriggered { get; }
    }
}
