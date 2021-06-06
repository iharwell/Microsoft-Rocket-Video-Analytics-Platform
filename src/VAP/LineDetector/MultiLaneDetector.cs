// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using BGSObjectDetector;
using System;
using System.Collections.Generic;
using System.Drawing;
using Utils;
using Utils.Items;
using Utils.ShapeTools;

namespace LineDetector
{
    // corresponds to LineCrossingBasedMultiLaneCounter.cs
    public class MultiLaneDetector
    {
        private Dictionary<string, ILineBasedDetector> laneDetector;

        /// <summary>
        ///   Creates a <see cref="MultiLaneDetector" /> object using the provided set of named
        ///   <see cref="ILineBasedDetector" /> objects.
        /// </summary>
        /// <param name="lineBasedDetector">
        ///   The named set of detectors used by this <see cref="MultiLaneDetector" />.
        /// </param>
        public MultiLaneDetector(Dictionary<string, ILineBasedDetector> lineBasedDetector)
        {
            laneDetector = lineBasedDetector;
        }

        /// <summary>
        ///   Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">
        ///   The index of the frame to process.
        /// </param>
        /// <param name="boxes">
        ///   A list of bounding boxes of items in frame.
        /// </param>
        /// <param name="mask">
        ///   A mask detailing the precise layout of items in the frame using black to indicate
        ///   vacant space, and white to indicate occupied space.
        /// </param>
        public void notifyFrameArrival(int frameNo, IList<IFramedItem> boxes, Bitmap mask)
        {

            foreach (KeyValuePair<string, ILineBasedDetector> entry in laneDetector)
            {
                entry.Value.notifyFrameArrival(frameNo, boxes, mask);
            }
        }

        /// <summary>
        ///   Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">
        ///   The index of the frame to process.
        /// </param>
        /// <param name="boxes">
        ///   A list of bounding boxes of items in frame.
        /// </param>
        /// <param name="mask">
        ///   A mask detailing the precise layout of items in the frame using black to indicate
        ///   vacant space, and white to indicate occupied space.
        /// </param>
        public void notifyFrameArrival( int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask )
        {

            foreach ( KeyValuePair<string, ILineBasedDetector> entry in laneDetector )
            {
                entry.Value.notifyFrameArrival( frameNo, boxes, mask );
            }
        }

        /// <summary>
        ///   Processes a frame upon arrival.
        /// </summary>
        /// <param name="frameNo">
        ///   The index of the frame to process.
        /// </param>
        /// <param name="mask">
        ///   A mask detailing the precise layout of items in the frame using black to indicate
        ///   vacant space, and white to indicate occupied space.
        /// </param>
        public void notifyFrameArrival(int frameNo, Bitmap mask)
        {

            foreach (KeyValuePair<string, ILineBasedDetector> entry in laneDetector)
            {
                entry.Value.notifyFrameArrival(frameNo, mask);
            }
        }

        /// <summary>
        ///   Gets the detection counts of each line used by this detector as of the latest frame.
        /// </summary>
        /// <returns>
        ///   Returns a <see cref="Dictionary{TKey, TValue}" /> of all occupancy counters, organized
        ///   by the name of the lines.
        /// </returns>
        public Dictionary<string, int> getCounts()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (KeyValuePair<string, ILineBasedDetector> entry in laneDetector)
            {
                counts.Add(entry.Key, entry.Value.getCount());
            }
            return counts;
        }

        /// <summary>
        ///   Gets the occupancy state of each line used by this detector as of the latest frame.
        /// </summary>
        /// <returns>
        ///   Returns a <see cref="Dictionary{TKey, TValue}" /> of all occupancy states, organized
        ///   by the name of the lines.
        /// </returns>
        public Dictionary<string, bool> getOccupancy()
        {
            Dictionary<string, bool> occupancy = new Dictionary<string, bool>();
            foreach (KeyValuePair<string, ILineBasedDetector> entry in laneDetector)
            {
                occupancy.Add(entry.Key, entry.Value.getOccupancy());
            }
            return occupancy;
        }

        /// <summary>
        ///   Gets the center of the bounding box of the requested line.
        /// </summary>
        /// <param name="laneID">
        ///   The name of the line to get the center of.
        /// </param>
        /// <returns>
        ///   Returns a <see cref="PointF"> with the value of the center of the requested line if it exists, and null otherwise.
        /// </returns>
        public PointF? getBboxCenter(string laneID)
        {
            foreach (KeyValuePair<string, ILineBasedDetector> entry in laneDetector)
            {
                if (entry.Key == laneID)
                {
                    StatisticRectangle rect = new StatisticRectangle(entry.Value.Bbox.ItemIDs);

                    return rect.Mean.Center();
                }
            }

            return null;
        }

        /// <summary>
        ///   Gets all lines used by this detector.
        /// </summary>
        /// <returns>
        ///   Returns a list of <c>Tuples</c> containing the name and coordinates of each line.
        /// </returns>
        public List<(string key, LineSegment segments)> getAllLines()
        {
            var lines = new List<(string key, LineSegment coordinates)>();
            foreach (KeyValuePair<string, ILineBasedDetector> lane in laneDetector)
            {
                var coor = lane.Value.getLineCoor()[0];
                lines.Add((lane.Key, coor));
            }
            return lines;
        }
    }
}
