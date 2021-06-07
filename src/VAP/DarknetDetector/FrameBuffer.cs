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
        private readonly Queue<Mat> _frameBuffer;
        private readonly int _bSize;

        public FrameBuffer(int size)
        {
            _bSize = size;
            _frameBuffer = new Queue<Mat>(_bSize);
        }

        public void Buffer(Mat frame)
        {
            _frameBuffer.Enqueue(frame);
            if (_frameBuffer.Count > _bSize)
                _frameBuffer.Dequeue();
        }

        public Mat[] ToArray()
        {
            return _frameBuffer.ToArray();
        }
    }
}
