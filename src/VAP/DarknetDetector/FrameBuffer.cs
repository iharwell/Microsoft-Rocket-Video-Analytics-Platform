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
        private readonly List<Mat> _frameBufferList;
        //private readonly Queue<Mat> _frameBuffer;
        private readonly int _bSize;

        public FrameBuffer(int size)
        {
            _bSize = size;
            // _frameBuffer = new Queue<Mat>(_bSize);
            _frameBufferList = new List<Mat>(_bSize);
        }

        public void Buffer(Mat frame)
        {
            /*
            if( _frameBuffer.Count > _bSize-1 )
            {
                _frameBuffer.Dequeue();
            }
            _frameBuffer.Enqueue(frame);
            */

            if( _frameBufferList.Count > _bSize -1)
            {
                _frameBufferList.RemoveAt(0);
            }
            _frameBufferList.Add(frame);
        }

        public Mat this[int index] => _frameBufferList[index];

        public Mat[] ToArray()
        {
            return _frameBufferList.ToArray();
        }
    }
}
