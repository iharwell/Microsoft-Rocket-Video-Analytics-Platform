// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using OpenCvSharp;

namespace Decoder
{
    public class Decoder
    {
        private VideoCapture _capture = null;
        private readonly string _inputURL;

        private readonly bool _toLoop;

        private int _objTotal, _objDirA, _objDirB;

        public Decoder(string input, bool loop)
        {
            _capture = new VideoCapture(input);
            _inputURL = input;

            _toLoop = loop;
        }

        public Mat GetNextFrame()
        {
            Mat sourceMat = new Mat();

            try
            {
                _capture.Read(sourceMat);
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("********RESET*****");

                _capture = new VideoCapture(_inputURL);

                return null;
            }

            if (sourceMat == null)
                return sourceMat;

            if (_toLoop)
            {
                if (sourceMat.Height == 0 && sourceMat.Width == 0)
                {
                    _capture = new VideoCapture(_inputURL);
                    _capture.Read(sourceMat);
                }
            }

            return sourceMat;
        }

        public int GetTotalFrameNum()
        {
            int length;
            length = (int)Math.Floor(_capture.Get(VideoCaptureProperties.FrameCount));

            return length;
        }

        public double GetVideoFPS()
        {
            double framerate;
            framerate = _capture.Get(VideoCaptureProperties.Fps);

            return framerate;
        }

        public void UpdateObjNum(int[] dirCount)
        {
            _objTotal = dirCount[0] + dirCount[1] + dirCount[2];
            _objDirA = dirCount[0];
            _objDirB = dirCount[1];
        }
    }
}
