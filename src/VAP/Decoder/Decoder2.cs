// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Decoder2;
using OpenCvSharp;

namespace Decoder
{
    public class Decoder2V
    {
        private Decoder2.Decoder inner = null;
        private readonly string _inputURL;

        private readonly bool _toLoop;

        private int _objTotal, _objDirA, _objDirB;
        private int queuesize = 50;

        private System.Collections.Concurrent.ConcurrentQueue<Mat> matQueue;

        public int TotalFrameNumber
        {
            get
            {
                return (int)inner.FrameCount;
            }
        }

        volatile int readFrames;

        public double FramesPerSecond => inner.FPS;

        public string FilePath => _inputURL;

        private Task queueTask;
        public Decoder2V(string input, double scale, bool loop)
        {
            inner = new Decoder2.Decoder(input, scale, LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_CUDA);
            _inputURL = input;
            matQueue = new ConcurrentQueue<Mat>();

            _toLoop = loop;
        }

        public void BeginReading()
        {
            queueTask = new Task(() => Run());
            queueTask.Start();
        }

        private void Run()
        {
            int frameNum = TotalFrameNumber;
            while ( readFrames < frameNum)
            {
                if (matQueue.Count < queuesize)
                {
                    matQueue.Enqueue(_internalGetFrame().Clone());
                    ++readFrames;
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        public Mat GetNextFrame()
        {
            if (queueTask != null)
            {
                int totalframes = TotalFrameNumber;
                do
                {
                    if (matQueue.TryDequeue(out Mat mat2))
                    {
                        return mat2;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                } while (readFrames < totalframes)
                    ;
            }
            return _internalGetFrame();
        }

        public Mat _internalGetFrame()
        {
            Mat sourceMat = new Mat();

            try
            {
                inner.GrabFrame();
                sourceMat = inner.GetNextFrame();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("********RESET*****");

                inner = new Decoder2.Decoder(_inputURL);

                return null;
            }

            if (sourceMat == null)
                return sourceMat;

            if (_toLoop)
            {
                if (sourceMat.Height == 0 && sourceMat.Width == 0)
                {
                    inner = new Decoder2.Decoder(_inputURL);
                    inner.GrabFrame();
                    sourceMat = inner.GetNextFrame();
                }
            }

            return sourceMat;
        }

        public int GetTotalFrameNum()
        {
            return TotalFrameNumber;
        }

        public double GetVideoFPS()
        {
            double framerate;
            framerate = inner.FPS;

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
