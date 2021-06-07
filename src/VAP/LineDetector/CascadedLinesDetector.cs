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
    /// Detector that uses a series of lines to detect the approach of items and fires when the items cross the final line.
    /// </summary>
    internal class CascadedLinesDetector : ILineBasedDetector
    {
        private readonly List<ISingleLineCrossingDetector> _lineCrossingDetectors;
        private readonly List<int> _minLags;
        private readonly List<int> _maxLags;
        private readonly int _noLines;
        private readonly List<List<int>> _crossingEventTimeStampBuffers;
        private int _count;
        private IFramedItem _bbox;
        private readonly int _supression_interval = 1;
        private readonly List<int> _lastEventFrame;
        private bool _debug = false;

        /// <summary>
        /// Activates debug logging.
        /// </summary>
        public void SetDebug()
        {
            _debug = true;
            foreach (ISingleLineCrossingDetector d in _lineCrossingDetectors)
            {
                d.SetDebug();
            }
        }

        /// <summary>
        /// Provides a history of the occupancy of this line detector, with each entry containing a list of occupancy values for each line considered by this detector.
        /// </summary>
        public List<List<double>> GetOccupancyHistory()
        {
            List<List<double>> ret = new List<List<double>>();
            if (_debug)
            {
                foreach (ISingleLineCrossingDetector lineDetector in _lineCrossingDetectors)
                {
                    ret.Add(lineDetector.GetLineOccupancyHistory());
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
            _lineCrossingDetectors = l_lineDetectors;
            _noLines = _lineCrossingDetectors.Count;
            _count = 0;
            _minLags = l_minLags;
            _maxLags = l_maxLags;

            _crossingEventTimeStampBuffers = new List<List<int>>();

            //the last line does not have a buffer at all!
            for (int i = 0; i < _noLines - 1; i++)
            {
                List<int> buffer = new List<int>();
                _crossingEventTimeStampBuffers.Add(buffer);
            }

            _lastEventFrame = new List<int>();
            for (int i = 0; i < _noLines - 1; i++)
            {
                _lastEventFrame.Add(0);
            }
        }


        private void PurgeOldEvents(int currentFrame, int lineNo)
        {
            while (_crossingEventTimeStampBuffers[lineNo].Count > 0)
            {
                if (_crossingEventTimeStampBuffers[lineNo][0] <= currentFrame - _maxLags[lineNo])
                {
                    _crossingEventTimeStampBuffers[lineNo].RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
        }


        private bool RecursiveCrossingEventCheck(int lineNo, int frameNo)
        {
            bool result = false;
            PurgeOldEvents(frameNo, lineNo - 1);
            if (_crossingEventTimeStampBuffers[lineNo - 1].Count > 0)
            {
                if (frameNo - _crossingEventTimeStampBuffers[lineNo - 1][0] > _minLags[lineNo - 1])
                {
                    if (frameNo - _lastEventFrame[lineNo - 1] >= _supression_interval)
                    {
                        if (lineNo - 1 == 0) //reached the source line - base case
                        {
                            result = true;
                        }
                        else
                        {
                            result = RecursiveCrossingEventCheck(lineNo - 1, frameNo);
                        }
                    }
                }
            }
            if (result)
            {
                _crossingEventTimeStampBuffers[lineNo - 1].RemoveAt(0);
                _lastEventFrame[lineNo - 1] = frameNo;
            }
            return result;
        }

        private void NotifyCrossingEvent(int frameNo, int lineNo)
        {
            if (lineNo != _noLines - 1)
            {
                PurgeOldEvents(frameNo, lineNo);
                _crossingEventTimeStampBuffers[lineNo].Add(frameNo);
            }
            else //this is the exit line
            {
                if (_noLines == 1)
                {
                    _count++;
                }
                else
                {
                    if (RecursiveCrossingEventCheck(lineNo, frameNo))
                    {
                        _count++;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void NotifyFrameArrival(int frameNo, IList<IFramedItem> boxes, Bitmap mask)
        {
            for (int i = 0; i < _noLines; i++)
            {
                (bool val, IFramedItem b) = _lineCrossingDetectors[i].NotifyFrameArrival(frameNo, boxes, mask);
                if (b != null)
                    _bbox = b;
                if (val)
                {
                    NotifyCrossingEvent(frameNo, i);
                }
            }
        }

        /// <inheritdoc/>
        public void NotifyFrameArrival(IFrame frame, int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask)
        {
            NotifyFrameArrival(frame, frameNo, boxes, mask, null);
        }

        /// <inheritdoc/>
        public void NotifyFrameArrival(IFrame frame, int frameNo, IList<IFramedItem> boxes, OpenCvSharp.Mat mask, object sourceObject)
        {
            for (int i = 0; i < _noLines; i++)
            {
                (bool val, IFramedItem b) = _lineCrossingDetectors[i].NotifyFrameArrival(frame, frameNo, boxes, mask, sourceObject);
                if (b != null)
                    _bbox = b;
                if (val)
                {
                    NotifyCrossingEvent(frame.FrameIndex, i);
                }
            }
        }

        /// <inheritdoc/>
        public void NotifyFrameArrival(int frameNo, Bitmap mask)
        {
            for (int i = 0; i < _noLines; i++)
            {
                bool val = _lineCrossingDetectors[i].NotifyFrameArrival(frameNo, mask);
                if (val)
                {
                    NotifyCrossingEvent(frameNo, i);
                }
            }
        }

        /// <summary>
        /// Gets the number of times that this detector has been triggered.
        /// </summary>
        public int GetCount()
        {
            return _count;
        }

        /// <summary>
        /// Gets the bounding box of the line used by this detector.
        /// </summary>
        public IFramedItem Bbox
        {
            get
            {
                return _bbox;
            }
        }

        /// <summary>
        /// Sets the count of this detector.
        /// </summary>
        public void SetCount(int value)
        {
            _count = value;
        }

        private int GetPendingNow(int frameNo, int lineNo)
        {
            PurgeOldEvents(frameNo, lineNo);
            return _crossingEventTimeStampBuffers[lineNo].Count;
        }

        /// <summary>
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> of the parameters used by this detector, stored by name.
        /// </summary>
        public Dictionary<string, object> GetParameters()
        {
            Dictionary<string, object> ret = new Dictionary<string, object>
            {
                { "LINES", _lineCrossingDetectors },
                { "MIN_LAGS", _minLags },
                { "MAX_LAGS", _maxLags }
            };
            return ret;
        }

        /// <summary>
        /// Gets the current occupancy state of this detector. This updates when the detector is notified of a frame arrival.
        /// </summary>
        /// <returns><see langword="true"/> if the line is occupied; otherwise, <see langword="false"/>.</returns>
        public bool GetOccupancy()
        {
            return _lineCrossingDetectors[0].GetOccupancy();
        }

        /// <summary>
        /// Gets the line segments used by this detector.
        /// </summary>
        public List<LineSegment> GetLineCoor()
        {
            List<LineSegment> coors = new List<LineSegment>();
            for (int i = 0; i < _lineCrossingDetectors.Count; i++)
            {
                coors.Add(_lineCrossingDetectors[i].GetDetectionLine().Line);
            }
            return coors;
        }

        /// <summary>
        /// Gets the <see cref="DetectionLine"/> used by this detector.
        /// </summary>
        public DetectionLine GetDetectionLine()
        {
            return _lineCrossingDetectors[0].GetDetectionLine();
        }
    }
}
