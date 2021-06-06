using System;
using System.Collections.Generic;
using System.Drawing;

using BGSObjectDetector;
using Utils;
using Utils.Items;

namespace LineDetector
{
    /// <summary>
    /// Detector that uses a series of lines to detect the approach of items and fires when the items cross the final line.
    /// </summary>
    internal class CascadedLinesDetector : ILineBasedDetector
    {
        private List<ISingleLineCrossingDetector> lineCrossingDetectors;
        private List<int> minLags;
        private List<int> maxLags;
        private int noLines;
        private List<List<int>> CrossingEventTimeStampBuffers;
        private int Count;
        private IFramedItem bbox;
        private int SUPRESSION_INTERVAL = 1;
        private List<int> lastEventFrame;
        private bool debug = false;

        /// <summary>
        /// Activates debug logging.
        /// </summary>
        public void setDebug()
        {
            debug = true;
            foreach (ISingleLineCrossingDetector d in lineCrossingDetectors)
            {
                d.setDebug();
            }
        }

        /// <summary>
        /// Provides a history of the occupancy of this line detector, with each entry containing a list of occupancy values for each line considered by this detector.
        /// </summary>
        public List<List<double>> getOccupancyHistory()
        {
            List<List<double>> ret = new List<List<double>>();
            if (debug)
            {
                foreach (ISingleLineCrossingDetector lineDetector in lineCrossingDetectors)
                {
                    ret.Add(lineDetector.getLineOccupancyHistory());
                }
                return ret;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a <see cref="CascadedLinesDetector"/> using the provided detectors, 
        /// </summary>
        /// <param name="l_lineDetectors">A list of <see cref="ISingleLineCrossingDetector"/> objects to use in this detector.</param>
        /// <param name="l_minLags">The minimum number of events to store in the internal buffer.</param>
        /// <param name="l_maxLags">The maximum number of events to store in the internal buffer.</param>
        public CascadedLinesDetector(List<ISingleLineCrossingDetector> l_lineDetectors, List<int> l_minLags, List<int> l_maxLags)
        {
            lineCrossingDetectors = l_lineDetectors;
            noLines = lineCrossingDetectors.Count;
            Count = 0;
            minLags = l_minLags;
            maxLags = l_maxLags;

            CrossingEventTimeStampBuffers = new List<List<int>>();

            //the last line does not have a buffer at all!
            for (int i = 0; i < noLines - 1; i++)
            {
                List<int> buffer = new List<int>();
                CrossingEventTimeStampBuffers.Add(buffer);
            }

            lastEventFrame = new List<int>();
            for (int i = 0; i < noLines - 1; i++)
            {
                lastEventFrame.Add(0);
            }
        }


        private void purgeOldEvents(int currentFrame, int lineNo)
        {
            while (CrossingEventTimeStampBuffers[lineNo].Count > 0)
            {
                if (CrossingEventTimeStampBuffers[lineNo][0] <= currentFrame - maxLags[lineNo])
                {
                    CrossingEventTimeStampBuffers[lineNo].RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
        }


        private bool recursiveCrossingEventCheck(int lineNo, int frameNo)
        {
            bool result = false;
            purgeOldEvents(frameNo, lineNo - 1);
            if (CrossingEventTimeStampBuffers[lineNo - 1].Count > 0)
            {
                if (frameNo - CrossingEventTimeStampBuffers[lineNo - 1][0] > minLags[lineNo - 1])
                {
                    if (frameNo - lastEventFrame[lineNo - 1] >= SUPRESSION_INTERVAL)
                    {
                        if (lineNo - 1 == 0) //reached the source line - base case
                        {
                            result = true;
                        }
                        else
                        {
                            result = recursiveCrossingEventCheck(lineNo - 1, frameNo);
                        }
                    }
                }
            }
            if (result)
            {
                CrossingEventTimeStampBuffers[lineNo - 1].RemoveAt(0);
                lastEventFrame[lineNo - 1] = frameNo;
            }
            return result;
        }

        private void NotifyCrossingEvent(int frameNo, int lineNo)
        {
            if (lineNo != noLines - 1)
            {
                purgeOldEvents(frameNo, lineNo);
                CrossingEventTimeStampBuffers[lineNo].Add(frameNo);
            }
            else //this is the exit line
            {
                if (noLines == 1)
                {
                    Count++;
                }
                else
                {
                    if (recursiveCrossingEventCheck(lineNo, frameNo))
                    {
                        Count++;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void notifyFrameArrival( int frameNo, IList<IFramedItem> boxes, Bitmap mask )
        {
            for ( int i = 0; i < noLines; i++ )
            {
                (bool val, IFramedItem b) = lineCrossingDetectors[i].notifyFrameArrival( frameNo, boxes, mask );
                if ( b != null )
                    bbox = b;
                if ( val )
                {
                    NotifyCrossingEvent( frameNo, i );
                }
            }
        }

        /// <inheritdoc/>
        public void notifyFrameArrival( int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask )
        {
            for ( int i = 0; i < noLines; i++ )
            {
                (bool val, IFramedItem b) = lineCrossingDetectors[i].notifyFrameArrival( frameNo, boxes, mask );
                if ( b != null )
                    bbox = b;
                if ( val )
                {
                    NotifyCrossingEvent( frameNo, i );
                }
            }
        }

        /// <inheritdoc/>
        public void notifyFrameArrival(int frameNo, Bitmap mask)
        {
            for (int i = 0; i < noLines; i++)
            {
                bool val = lineCrossingDetectors[i].notifyFrameArrival(frameNo, mask);
                if (val)
                {
                    NotifyCrossingEvent(frameNo, i);
                }
            }
        }

        /// <summary>
        /// Gets the number of times that this detector has been triggered.
        /// </summary>
        public int getCount()
        {
            return Count;
        }

        /// <summary>
        /// Gets the bounding box of the line used by this detector.
        /// </summary>
        public IFramedItem Bbox
        {
            get
            {
                return bbox;
            }
        }

        /// <summary>
        /// Sets the count of this detector.
        /// </summary>
        public void setCount(int value)
        {
            Count = value;
        }

        private int getPendingNow(int frameNo, int lineNo)
        {
            purgeOldEvents(frameNo, lineNo);
            return CrossingEventTimeStampBuffers[lineNo].Count;
        }

        /// <summary>
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> of the parameters used by this detector, stored by name.
        /// </summary>
        public Dictionary<string, Object> getParameters()
        {
            Dictionary<string, Object> ret = new Dictionary<string, object>();
            ret.Add("LINES", lineCrossingDetectors);
            ret.Add("MIN_LAGS", minLags);
            ret.Add("MAX_LAGS", maxLags);
            return ret;
        }

        /// <summary>
        /// Gets the current occupancy state of this detector. This updates when the detector is notified of a frame arrival.
        /// </summary>
        /// <returns><see langword="true"/> if the line is occupied; otherwise, <see langword="false"/>.</returns>
        public bool getOccupancy()
        {
            return lineCrossingDetectors[0].getOccupancy();
        }

        /// <summary>
        /// Gets the line segments used by this detector.
        /// </summary>
        public List<LineSegment> getLineCoor()
        {
            List<LineSegment> coors = new List<LineSegment>();
            for (int i = 0; i < lineCrossingDetectors.Count; i++)
            {
                coors.Add( lineCrossingDetectors[i].getDetectionLine().Line );
            }
            return coors;
        }

        /// <summary>
        /// Gets the <see cref="DetectionLine"/> used by this detector.
        /// </summary>
        public DetectionLine getDetectionLine()
        {
            return lineCrossingDetectors[0].getDetectionLine();
        }
    }
}
