// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using OpenCvSharp;

namespace DarknetDetector
{
    internal class FrameBuffer
    {
        private readonly List<(Mat, int)> _frameBufferList;
        //private readonly Queue<Mat> _frameBuffer;
        private readonly int _bSize;

        public FrameBuffer(int size)
        {
            _bSize = size;
            // _frameBuffer = new Queue<Mat>(_bSize);
            _frameBufferList = new List<(Mat,int)>(_bSize);
        }

        public int Buffer(Mat frame, int frameNumber)
        {
            /*
            if( _frameBuffer.Count > _bSize-1 )
            {
                _frameBuffer.Dequeue();
            }
            _frameBuffer.Enqueue(frame);
            */
            int retNum = -1;
            if( _frameBufferList.Count > _bSize -1)
            {
                retNum = _frameBufferList[0].Item2;
                _frameBufferList.RemoveAt(0);

            }
            _frameBufferList.Add((frame,frameNumber));
            return retNum;
        }

        public Mat GetByFrameNumber( int frameNumber )
        {
            for (int i = 0; i < _frameBufferList.Count; i++)
            {
                if( _frameBufferList[i].Item2 == frameNumber )
                {
                    return _frameBufferList[i].Item1;
                }
            }
            return null;
        }

        public Mat this[int index] =>  _frameBufferList[index].Item1;
    }
}
