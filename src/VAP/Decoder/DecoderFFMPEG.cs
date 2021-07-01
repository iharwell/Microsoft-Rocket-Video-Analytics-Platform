// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Decoder2;
using OpenCvSharp;
using Utils.Items;

namespace Decoder
{
    public class DecoderFFMPEG : IDecoder
    {
        private static readonly string[] Extensions = new string[]
        {
            "mp4",
            "mov",
            "wmv",
            "mkv"
        };

        private readonly bool _toLoop;
        private Task _fileTask;
        private Task _disposalTask;
        private volatile Decoder2.Decoder _inner;
        private volatile Decoder2.Decoder _innerNext;

        private ConcurrentBag<Decoder2.Decoder> _disposables;
        private ConcurrentQueue<Frame> _matQueue;
        private volatile bool _nextFileReady;
        private int _queuesize = 750;
        private Task _queueTask;
        private volatile int _readFrames;
        private double _scale;
        private SpinLock _spinner;

        public DecoderFFMPEG(string input, double scale, bool loop)
        {
            _matQueue = new ConcurrentQueue<Frame>();
            _disposables = new ConcurrentBag<Decoder2.Decoder>();
            PathIndex = 0;
            FilePaths = new List<string>();
            _toLoop = loop;
            this._scale = scale;
            _nextFileReady = false;
            _spinner = new SpinLock();

            if (input != null)
            {
                _inner = new Decoder2.Decoder(input, scale, LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_NV12);
                FilePaths.Add(input);
            }
            else
            {
                _inner = null;
                _innerNext = null;
            }
        }

        private DecoderFFMPEG(double scale, bool loop)
            : this(null, scale, loop)
        { }

        public string FilePath => FilePaths[PathIndex];

        public IList<string> FilePaths { get; }

        public double FramesPerSecond => GetVideoFPS();

        public bool HasMoreFrames => (PathIndex < FilePaths.Count) || _matQueue.Count > 0;

        public int PathIndex { get; protected set; }

        public int TotalFrameNumber
        {
            get
            {
                int val;
                bool gotLock = false;
                try
                {
                    _spinner.Enter(ref gotLock);
                    val = (int)_inner.FrameCount;
                }
                finally
                {
                    if (gotLock)
                    {
                        _spinner.Exit();
                    }
                }
                return val;
            }
        }

        public delegate DateTime TimeParser(IFrame frame);

        public delegate string NameParser(IFrame frame);

        public TimeParser TimeStampParser { get; set; }
        public NameParser CameraNameParser { get; set; }

        public static DecoderFFMPEG GetDirectoryDecoder(string folder, double scale)
        {
            if (folder[^4] == '.')
            {
                return new DecoderFFMPEG(folder, scale, false);
            }
            var files = Directory.GetFiles(folder);
            List<string> filtered = new List<string>();
            foreach (var file in files)
            {
                if (Extensions.Contains(file[^3..]))
                {
                    filtered.Add(file);
                }
            }
            if (filtered.Count > 0)
            {
                var decoder = new DecoderFFMPEG(scale, false);
                for (int i = 0; i < filtered.Count; i++)
                {
                    decoder.FilePaths.Add(filtered[i]);
                }
                decoder._inner = new Decoder2.Decoder(decoder.FilePath, scale, LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_P016LE);
                return decoder;
            }
            return null;
        }

        public Mat _internalGetFrame()
        {
            Mat sourceMat;
            try
            {
                if (_readFrames >= TotalFrameNumber)
                {
                    if (!NextFile())
                    {
                        return null;
                    }
                }
                _inner.GrabFrame();
                sourceMat = _inner.GetNextFrame();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("********RESET*****");

                _inner = new Decoder2.Decoder(FilePath);

                return null;
            }

            if (sourceMat == null)
                return sourceMat;

            if (_toLoop)
            {
                if (sourceMat.Height == 0 && sourceMat.Width == 0)
                {
                    _inner = new Decoder2.Decoder(FilePath, _scale, LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_P016LE);
                    _inner.GrabFrame();
                    sourceMat = _inner.GetNextFrame();
                }
            }

            return sourceMat;
        }

        public void BeginReading()
        {
            _fileTask = new Task(() => PreloadFiles());
            _fileTask.Start();
            _queueTask = new Task(() => RunDecode());
            _queueTask.Start();
            _disposalTask = new Task(() => TrashDisposal());
            _disposalTask.Start();
        }

        public IFrame GetNextFrame()
        {
            var f = GetNextImage();
            if (f.FrameData == null)
            {
                return null;
            }
            return f;
        }

        internal IFrame GetNextImage()
        {
            if (_queueTask != null)
            {
                // int totalframes = TotalFrameNumber;
                do
                {
                    if (_matQueue.TryDequeue(out Frame mat2))
                    {
                        return mat2;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                } while (HasMoreFrames)
                    ;
            }
            Frame f = new Frame
            {
                FrameData = _internalGetFrame(),
                SourceName = FilePath
            };
            if (TimeStampParser != null)
            {
                f.TimeStamp = TimeStampParser(f);
            }
            if (CameraNameParser != null)
            {
                f.CameraName = CameraNameParser(f);
            }
            return f;
        }

        public int GetTotalFrameNum()
        {
            return TotalFrameNumber;
        }

        public double GetVideoFPS()
        {
            double framerate;
            bool lockTaken = false;
            try
            {
                _spinner.Enter(ref lockTaken);
                framerate = _inner.FPS;
            }
            finally
            {
                if (lockTaken)
                {
                    _spinner.Exit();
                }
            }
            return framerate;
        }

        private bool NextFile()
        {
            if (PathIndex < FilePaths.Count - 1)
            {
                Decoder2.Decoder tmp;
                if (_fileTask == null)
                {
                    tmp = new Decoder2.Decoder(FilePath, _scale, LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_P016LE);
                }
                else
                {
                    while (!_nextFileReady)
                    {
                        Thread.Sleep(1);
                    }
                    tmp = _innerNext;
                    _innerNext = null;
                    _nextFileReady = false;
                }
                int preCount = _matQueue.Count;
                bool lockGot = false;
                Decoder2.Decoder tmpDecoder = null;
                try
                {
                    _spinner.Enter(ref lockGot);
                    ++PathIndex;
                    _inner.Close();
                    tmpDecoder = (_inner);
                    _inner = tmp;
                    _readFrames = 0;
                }
                finally
                {
                    if (lockGot)
                    {
                        _spinner.Exit();
                    }
                }
                if (tmpDecoder != null)
                {
                    _disposables.Add(tmpDecoder);
                }
                int postCount = _matQueue.Count;
                Console.WriteLine("\tPreQ: " + preCount + "\tPostQ: " + postCount);
                return true;
            }
            else
            {
                ++PathIndex;
                return false;
            }
        }

        private void PreloadFiles()
        {
            while (true)
            {
                if (!_nextFileReady && (TotalFrameNumber - _readFrames) < (_queuesize / 4))
                {
                    if (PathIndex < FilePaths.Count - 1)
                    {
                        _innerNext = new Decoder2.Decoder(FilePaths[PathIndex + 1], _scale, LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_P016LE);
                        _nextFileReady = true;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void RunDecode()
        {
            Mat image;
            bool notified = false;
            while (true)
            {
                if (_matQueue.Count < _queuesize)
                {
                    notified = false;
                    image = _internalGetFrame();
                    ++_readFrames;
                    if (image == null)
                    {
                        break;
                    }
                    Frame f = new Frame
                    {
                        SourceName = FilePath,
                        FrameData = image.Clone()
                    };
                    _matQueue.Enqueue(f);
                }
                else
                {
                    if (!notified)
                    {
                        notified = true;
                        //Console.WriteLine("Queue full: " + queuesize + " elements.");
                    }
                    Thread.Sleep(150);
                }
            }
        }

        private void TrashDisposal()
        {
            while (!_fileTask.IsCompleted)
            {
                if (_disposables.TryTake(out Decoder2.Decoder result))
                {
                    result.Dispose();
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        public Mat GetNextFrameImage()
        {
            var f = GetNextFrame();
            if (f == null)
            {
                return null;
            }
            return f.FrameData;
        }
    }
}
