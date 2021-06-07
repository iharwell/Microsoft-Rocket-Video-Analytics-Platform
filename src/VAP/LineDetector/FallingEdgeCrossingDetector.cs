// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace LineDetector
{
    /// <summary>
    /// A line crossing detector that fires when an item leaves the line area.
    /// </summary>
    internal class FallingEdgeCrossingDetector : ICrossingDetector
    {
        private readonly List<int> _frameNoList = new List<int>();
        private readonly List<double> _occupancyValueList = new List<double>();
        private readonly int _upStateTransitionLength = 4;
        private readonly int _downStateTransitionLength = 10;
        private readonly int _history;
        private OCCUPANCY_STATE _curState = OCCUPANCY_STATE.UNOCCUPIED;
        private bool _debug = false;
        public List<double> _debug_occupancySequence;

        /// <summary>
        /// Enables debug mode. Within <see cref="FallingEdgeCrossingDetector"/>, this enables logging occupancy values.
        /// </summary>
        /// <remarks>
        /// The occupancy log may be accessed with <see cref="GetLineOccupancyHistory"/>.</remarks>
        public void SetDebug()
        {
            _debug = true;
            _debug_occupancySequence = new List<double>();
        }

        /// <summary>
        /// Creates a <see cref="FallingEdgeCrossingDetector"/> with the provided frame rate sampling factor.
        /// </summary>
        /// <param name="sFactor">The frame rate sampling factor. Note that particularly large values could behave unexpectedly.</param>
        public FallingEdgeCrossingDetector(int sFactor)
        {
            _upStateTransitionLength = (int)Math.Ceiling((double)_upStateTransitionLength / sFactor);
            _downStateTransitionLength = (int)Math.Ceiling((double)_downStateTransitionLength / sFactor);
            _history = Math.Max(_upStateTransitionLength, _downStateTransitionLength);
        }

        private bool CheckForStateTransision()
        {
            if (_occupancyValueList.Count < _history)
            {
                return false;
            }
            if (_curState == OCCUPANCY_STATE.UNOCCUPIED)
            {
                for (int i = 0; i < _upStateTransitionLength; i++)
                {
                    if (_occupancyValueList[_occupancyValueList.Count - i - 1] < 0)
                    {
                        return false;
                    }
                }
                _curState = OCCUPANCY_STATE.OCCUPIED;
                return false;
            }
            else
            {
                for (int i = 0; i < _downStateTransitionLength; i++)
                {
                    if (_occupancyValueList[_occupancyValueList.Count - i - 1] > 0)
                    {
                        return false;
                    }
                }
                _curState = OCCUPANCY_STATE.UNOCCUPIED;
                return true;
            }
        }


        //return true if there was a line crossing event
        /// <summary>
        /// Notifies the detector of a new occupancy state at a given frame.
        /// </summary>
        /// <param name="frameNo">The index of the frame of interest.</param>
        /// <param name="occupancy">The occupancy state at that frame.</param>
        /// <returns><see langword="true"/> if an event was detected, and <see langword="false"/> otherwise.</returns>
        public bool NotifyOccupancy(int frameNo, bool occupancy)
        {
            while (_frameNoList.Count > 0)
            {
                if (_frameNoList[0] <= frameNo - _history)
                {
                    _frameNoList.RemoveAt(0);
                    _occupancyValueList.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            double finalOccupancyValue = -1;
            if (occupancy)
            {
                finalOccupancyValue = 1;
            }

            //interpolate for missing frames
            if (_frameNoList.Count > 0)
            {
                int curFrameNo = _frameNoList[^1];
                int nextFrameNo = frameNo;
                int diff = nextFrameNo - curFrameNo;
                if (diff > 1)
                {
                    double initialOccupancyValue = _occupancyValueList[^1];
                    double occupancyDiff = finalOccupancyValue - initialOccupancyValue;
                    double ratio = occupancyDiff / (double)diff;
                    for (int f = curFrameNo + 1; f < nextFrameNo; f++)
                    {
                        double value = (f - curFrameNo) * ratio + initialOccupancyValue;
                        _frameNoList.Add(f);
                        _occupancyValueList.Add(value);
                        if (_debug)
                        {
                            _debug_occupancySequence.Add(value);
                        }
                    }
                }
            }
            _frameNoList.Add(frameNo);
            _occupancyValueList.Add(finalOccupancyValue);
            if (_debug)
            {
                _debug_occupancySequence.Add(finalOccupancyValue);
            }

            //Console.WriteLine("finalOccupancyValue:" + finalOccupancyValue);
            return CheckForStateTransision();
        }

        /// <summary>
        /// Gets the occupancy state of the detector as of the latest frame.
        /// </summary>
        public OCCUPANCY_STATE GetState()
        {
            return _curState;
        }

        /// <summary>
        /// Gets a list of all occupancy values observed by the detector while debugging has been enabled. No frame indices are included.
        /// </summary>
        public List<double> GetLineOccupancyHistory()
        {
            return _debug_occupancySequence;
        }
    }
}
