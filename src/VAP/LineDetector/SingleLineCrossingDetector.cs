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
        private DetectionLine line;
        private bool occupancy;
        private IFramedItem bbox;
        private FallingEdgeCrossingDetector lineCrossingDetector;
        private bool debug = false;

        /// <inheritdoc cref="SingleLineCrossingDetector(int, int, int, int, double, int)"/>
        public SingleLineCrossingDetector(int a, int b, int c, int d, string lineName)
        {
            line = new DetectionLine(a, b, c, d, lineName);
            lineCrossingDetector = new FallingEdgeCrossingDetector(1);
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
            line = new DetectionLine(a, b, c, d, threshold, lineName );
            lineCrossingDetector = new FallingEdgeCrossingDetector(sFactor);
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
        public (bool crossingResult, IFramedItem b) notifyFrameArrival( int frameNo, IList<IFramedItem> boxes, Bitmap mask)
        {
            (occupancy, bbox) = line.isOccupied(boxes, mask);
            bool crossingResult = lineCrossingDetector.notifyOccupancy(frameNo, occupancy);
            return (crossingResult, bbox);
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
        public (bool crossingResult, IFramedItem b) notifyFrameArrival( IFrame frame, int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask )
        {
            return notifyFrameArrival( frame, frameNo, boxes, mask, null );
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
        public (bool crossingResult, IFramedItem b) notifyFrameArrival( IFrame frame, int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask, object signature )
        {
            (occupancy, bbox) = line.isOccupied( boxes, mask, signature );
            bool crossingResult = lineCrossingDetector.notifyOccupancy(frameNo, occupancy);
            if( bbox != null && bbox.ItemIDs[bbox.ItemIDs.Count - 1] is ITriggeredItem trig )
            {
                trig.FurtherAnalysisTriggered = crossingResult;
            }
            if( crossingResult && bbox == null )
            {
                ILineTriggeredItemID item = new LineTriggeredItemID( line.Line.BoundingBox, 0, null, 0, 0, nameof(SingleLineCrossingDetector) );
                item.SourceObject = signature;
                //item.TriggerLine = this.line.LineName;
                item.FurtherAnalysisTriggered = true;
                bbox = new FramedItem( frame, item );

                boxes.Add( bbox );
            }
            /*if ( !crossingResult && bbox != null && bbox.ItemIDs[bbox.ItemIDs.Count-1].SourceObject == signature )
            {
                bbox.ItemIDs.RemoveAt( bbox.ItemIDs.Count - 1 );
            }*/
            return (crossingResult, bbox);
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
            occupancy = line.isOccupied(mask);
            bool crossingResult = lineCrossingDetector.notifyOccupancy(frameNo, occupancy);
            return crossingResult;
        }

        /// <summary>
        /// Gets the occupancy state of this detector as of the latest frame.
        /// </summary>
        public OCCUPANCY_STATE getState()
        {
            return lineCrossingDetector.getState();
        }

        /// <summary>
        /// Enables debug logging.
        /// </summary>
        public void setDebug()
        {
            debug = true;
            lineCrossingDetector.setDebug();
        }

        /// <summary>
        /// Gets the line occupancy overlap values which are stored while in debug mode.
        /// </summary>
        public List<double> getLineOccupancyHistory()
        {
            if (debug)
            {
                return lineCrossingDetector.getLineOccupancyHistory();
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
            return occupancy;
        }

        /// <summary>
        /// Gets the <see cref="DetectionLine"/> used by this detector.
        /// </summary>
        public DetectionLine getDetectionLine()
        {
            return line;
        }

        /// <summary>
        /// Gets the bounding box of this detector's region of interest.
        /// </summary>
        public IFramedItem getBbox()
        {
            return bbox;
        }

        /// <summary>
        /// Gets the coordinates of the line used by this detector.
        /// </summary>
        public (Point p1, Point p2) getLineCoor()
        {
            return (getDetectionLine().p1, getDetectionLine().p2);
        }
    }
}
