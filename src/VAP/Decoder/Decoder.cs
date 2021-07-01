﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using OpenCvSharp;

namespace Decoder
{
    public class Decoder
    {
        VideoCapture capture = null;
        string inputURL;

        bool toLoop;

        public Decoder(string input, bool loop)
        {
            capture = new VideoCapture(input);
            inputURL = input;

            toLoop = loop;
        }

        public Mat getNextFrame()
        {
            Mat sourceMat = new Mat();

            try
            {
                capture.Read(sourceMat);
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("********RESET*****");

                capture = new VideoCapture(inputURL);

                return null;
            }

            if (sourceMat == null)
                return sourceMat;

            if (toLoop)
            {
                if (sourceMat.Height == 0 && sourceMat.Width == 0)
                {
                    capture = new VideoCapture(inputURL);
                    capture.Read(sourceMat);
                }
            }

            return sourceMat;
        }

        public int getTotalFrameNum()
        {
            int length;
            length = (int)Math.Floor(capture.Get(VideoCaptureProperties.FrameCount));

            return length;
        }

        public double getVideoFPS()
        {
            double framerate;
            framerate = capture.Get(VideoCaptureProperties.Fps);

            return framerate;
        }
    }
}
