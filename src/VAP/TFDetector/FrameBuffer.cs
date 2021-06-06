// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

﻿using System;
using System.Collections.Generic;
using System.Text;

using OpenCvSharp;

namespace TFDetector
{
    internal class FrameBuffer
    {
        private Queue<Mat> frameBuffer;
        private int bSize;

        public FrameBuffer(int size)
        {
            bSize = size;
            frameBuffer = new Queue<Mat>(bSize);
        }

        public void Buffer(Mat frame)
        {
            frameBuffer.Enqueue(frame);
            if (frameBuffer.Count > bSize)
                frameBuffer.Dequeue();
        }

        public Mat[] ToArray()
        {
            return frameBuffer.ToArray();
        }
    }
}
