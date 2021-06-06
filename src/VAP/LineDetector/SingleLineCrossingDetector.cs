// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using BGSObjectDetector;
using System.Collections.Generic;
using System.Drawing;
using Utils.Items;

namespace LineDetector
{
    public class SingleLineCrossingDetector : ISingleLineCrossingDetector
    {
        private readonly DetectionLine _line;
        private bool _occupancy;
        private IFramedItem _bbox;
        private readonly FallingEdgeCrossingDetector _lineCrossingDetector;
        private bool _debug = false;

        /// <inheritdoc cref="SingleLineCrossingDetector(int, int, int, int, double, int)"/>
        public SingleLineCrossingDetector(int a, int b, int c, int d, string lineName)
        {
            _line = new DetectionLine(a, b, c, d, lineName);
            _lineCrossingDetector = new FallingEdgeCrossingDetector(1);
        }

        /// <summary>
        /// Creates a <see cref="SingleLineCrossingDetector"/> using the provided coordinates for
        /// the start and end point of the line.
        /// </summary>
        /// <param name="a">The X coordinate of the first point of the line.</param>
        /// <param name="b">The Y coordinate of the first point of the line.</param>
        /// <param name="c">The X coordinate of the second point of the line.</param>
        /// <param name="d">The Y coordinate of the second point of the line.</param>
        /// <param name="threshold">
        /// The overlap fraction threshold for this detector to be considered occupied.
        /// </param>
        /// <param name="sFactor">The frame rate sampling factor.</param>
        public SingleLineCrossingDetector(int a, int b, int c, int d, double threshold, int sFactor, string lineName)
        {
            _line = new DetectionLine(a, b, c, d, threshold, lineName);
            _lineCrossingDetector = new FallingEdgeCrossingDetector(sFactor);
        }

        /// <summary>
        /// Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">The index of the frame to process.</param>
        /// <param name="boxes">A list of bounding boxes of items in frame.</param>
        /// <param name="mask">
        /// A mask detailing the precise layout of items in the frame using black to indicate vacant
        /// space, and white to indicate occupied space.
        /// </param>
        /// <returns>
        /// Returns a Tuple that contains a boolean indicating whether a crossing was detected, and
        /// the bounding box of the crossing item.
        /// </returns>
        public (bool crossingResult, IFramedItem b) notifyFrameArrival(int frameNo, IList<IFramedItem> boxes, Bitmap mask)
        {
            (_occupancy, _bbox) = _line.IsOccupied(boxes, mask);
            bool crossingResult = _lineCrossingDetector.notifyOccupancy(frameNo, _occupancy);
            return (crossingResult, _bbox);
        }

        /// <summary>
        /// Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">The index of the frame to process.</param>
        /// <param name="boxes">A list of bounding boxes of items in frame.</param>
        /// <param name="mask">
        /// A mask detailing the precise layout of items in the frame using black to indicate vacant
        /// space, and white to indicate occupied space.
        /// </param>
        /// <returns>
        /// Returns a Tuple that contains a boolean indicating whether a crossing was detected, and
        /// the bounding box of the crossing item.
        /// </returns>
        public (bool crossingResult, IFramedItem b) notifyFrameArrival(IFrame frame, int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask)
        {
            return notifyFrameArrival(frame, frameNo, boxes, mask, null);
        }

        /// <summary>
        /// Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">The index of the frame to process.</param>
        /// <param name="boxes">A list of bounding boxes of items in frame.</param>
        /// <param name="mask">
        /// A mask detailing the precise layout of items in the frame using black to indicate vacant
        /// space, and white to indicate occupied space.
        /// </param>
        /// <returns>
        /// Returns a Tuple that contains a boolean indicating whether a crossing was detected, and
        /// the bounding box of the crossing item.
        /// </returns>
        public (bool crossingResult, IFramedItem b) notifyFrameArrival(IFrame frame, int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask, object signature)
        {
            (_occupancy, _bbox) = _line.IsOccupied(boxes, mask, signature);
            bool crossingResult = _lineCrossingDetector.notifyOccupancy(frameNo, _occupancy);
            if (_bbox != null && _bbox.ItemIDs[_bbox.ItemIDs.Count - 1] is ITriggeredItem trig)
            {
                trig.FurtherAnalysisTriggered = crossingResult;
            }
            if (crossingResult && _bbox == null)
            {
                ILineTriggeredItemID item = new LineTriggeredItemID(_line.Line.BoundingBox, 0, null, 0, 0, nameof(SingleLineCrossingDetector))
                {
                    SourceObject = signature,
                    TriggerLine = this._line.LineName,
                    FurtherAnalysisTriggered = true
                };
                _bbox = new FramedItem(frame, item);

                boxes.Add(_bbox);
            }
            /*if ( !crossingResult && bbox != null && bbox.ItemIDs[bbox.ItemIDs.Count-1].SourceObject == signature )
            {
                bbox.ItemIDs.RemoveAt( bbox.ItemIDs.Count - 1 );
            }*/
            return (crossingResult, _bbox);
        }

        /// <summary>
        /// Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">The index of the frame to process.</param>
        /// <param name="mask">
        /// A mask detailing the precise layout of items in the frame using black to indicate vacant
        /// space, and white to indicate occupied space.
        /// </param>
        /// <returns>Returns a boolean indicating whether a crossing was detected.</returns>
        public bool notifyFrameArrival(int frameNo, Bitmap mask)
        {
            _occupancy = _line.IsOccupied(mask);
            bool crossingResult = _lineCrossingDetector.notifyOccupancy(frameNo, _occupancy);
            return crossingResult;
        }

        /// <summary>
        /// Gets the occupancy state of this detector as of the latest frame.
        /// </summary>
        public OCCUPANCY_STATE getState()
        {
            return _lineCrossingDetector.getState();
        }

        /// <summary>
        /// Enables debug logging.
        /// </summary>
        public void setDebug()
        {
            _debug = true;
            _lineCrossingDetector.setDebug();
        }

        /// <summary>
        /// Gets the line occupancy overlap values which are stored while in debug mode.
        /// </summary>
        public List<double> getLineOccupancyHistory()
        {
            if (_debug)
            {
                return _lineCrossingDetector.getLineOccupancyHistory();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the occupancy state of this detector as of the latest frame.
        /// </summary>
        /// <returns>
        /// Returns true if the detector is occupied by one or more items, and false otherwise.
        /// </returns>
        public bool getOccupancy()
        {
            return _occupancy;
        }

        /// <summary>
        /// Gets the <see cref="DetectionLine"/> used by this detector.
        /// </summary>
        public DetectionLine getDetectionLine()
        {
            return _line;
        }

        /// <summary>
        /// Gets the bounding box of this detector's region of interest.
        /// </summary>
        public IFramedItem GetBbox()
        {
            return _bbox;
        }

        /// <summary>
        /// Gets the coordinates of the line used by this detector.
        /// </summary>
        public (Point p1, Point p2) GetLineCoor()
        {
            return (getDetectionLine().P1, getDetectionLine().P2);
        }
    }
}
