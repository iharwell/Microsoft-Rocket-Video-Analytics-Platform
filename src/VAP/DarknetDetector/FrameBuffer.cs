// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using OpenCvSharp;
using Utils.Items;

namespace DarknetDetector
{
    internal class FrameBuffer
    {
        private readonly List<IFrame> _frameBufferList;
        //private readonly Queue<Mat> _frameBuffer;
        private readonly int _bSize;

        public FrameBuffer(int size)
        {
            _bSize = size;
            // _frameBuffer = new Queue<Mat>(_bSize);
            _frameBufferList = new List<IFrame>(_bSize+1);
        }

        public int Buffer(IFrame frame)
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
                retNum = _frameBufferList[0].FrameIndex;
                _frameBufferList.RemoveAt(0);

            }
            _frameBufferList.Add(frame);
            return retNum;
        }

        public IFrame GetByFrameNumber( int frameNumber )
        {
            int minFrame = _frameBufferList[0].FrameIndex;
            if (_frameBufferList[frameNumber - minFrame].FrameIndex == frameNumber)
            {
                return _frameBufferList[frameNumber - minFrame];
            }

            for (int i = 0; i < _frameBufferList.Count; i++)
            {
                if (_frameBufferList[i].FrameIndex == frameNumber)
                {
                    return _frameBufferList[i];
                }
            }
            return null;
        }

        public IFrame this[int index] =>  _frameBufferList[index];
    }
}
