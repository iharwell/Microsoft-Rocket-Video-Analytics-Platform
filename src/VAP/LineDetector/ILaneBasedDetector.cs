// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;

using BGSObjectDetector;
using Utils;
using Utils.Items;

namespace LineDetector
{
    /// <summary>
    /// A detector that checks for items crossing a line.
    /// </summary>
    public interface ILineBasedDetector
    {
        /// <summary>
        /// Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">The index of the frame to process.</param>
        /// <param name="boxes">A list of bounding boxes of items in frame.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        void NotifyFrameArrival(int frameNo, IList<IFramedItem> boxes, Bitmap mask);

        /// <summary>
        /// Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">The index of the frame to process.</param>
        /// <param name="boxes">A list of bounding boxes of items in frame.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        void NotifyFrameArrival(IFrame frame, int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask);

        /// <summary>
        /// Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">The index of the frame to process.</param>
        /// <param name="boxes">A list of bounding boxes of items in frame.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        void NotifyFrameArrival(IFrame frame, int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask, object signature);

        /// <summary>
        /// Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">The index of the frame to process.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        void NotifyFrameArrival(int frameNo, Bitmap mask);

        //TODO(iharwell): This should be moved somewhere more appropriate.
        /// <summary>
        /// Activates debug logging.
        /// </summary>
        void SetDebug();

        /// <summary>
        /// Provides a history of the occupancy of this line detector, with each entry containing a list of occupancy values for each line considered by this detector.
        /// </summary>
        List<List<double>> GetOccupancyHistory();

        /// <summary>
        /// Gets the <c>DetectionLine</c> used by this detector.
        /// </summary>
        DetectionLine GetDetectionLine();

        /// <summary>
        /// Gets the current occupancy state of this detector. This updates when the detector is notified of a frame arrival.
        /// </summary>
        /// <returns>Returns true if the line is occupied, and false otherwise.</returns>
        bool GetOccupancy();

        /// <summary>
        /// Gets the number of times that this detector has been triggered.
        /// </summary>
        int GetCount();

        //TODO(iharwell): This seems like it should not be part of the interface.
        /// <summary>
        /// Sets the count of this detector.
        /// </summary>
        void SetCount(int value);

        /// <summary>
        /// Gets the bounding box of the item that triggered this detector in the latest frame.
        /// </summary>
        IFramedItem Bbox { get; }

        /// <summary>
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> of the parameters used by this detector, stored by name.
        /// </summary>
        Dictionary<string, Object> GetParameters();

        /// <summary>
        /// Gets the line segments used by this detector.
        /// </summary>
        List<LineSegment> GetLineCoor();
    }
}
