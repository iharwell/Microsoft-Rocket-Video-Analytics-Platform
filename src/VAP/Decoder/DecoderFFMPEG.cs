// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibAvSharp.Native;
using OpenCvSharp;
using Utils.Items;

namespace Decoder
{
    public class DecoderFFMPEG : IDecoder, IDisposable, ISerializable
    {

        // private const AVPixelFormat TARGET_FORMAT = AVPixelFormat.AV_PIX_FMT_CUDA;
        // private const AVPixelFormat TARGET_FORMAT = AVPixelFormat.AV_PIX_FMT_NV12;
        // private const AVPixelFormat TARGET_FORMAT = AVPixelFormat.AV_PIX_FMT_P010LE;
        // private const AVPixelFormat TARGET_FORMAT = AVPixelFormat.AV_PIX_FMT_P016LE;
        private const AVPixelFormat TARGET_FORMAT = AVPixelFormat.AV_PIX_FMT_NONE;
        private static readonly string[] Extensions = new string[]
        {
            "mp4",
            "m4v",
            "mov",
            "wmv",
            "mkv"
        };

        private readonly bool _toLoop;
        private ConcurrentBag<LibAvSharp.Decoder> _disposables;
        private Task _fileTask;
        private Task _queueTask;
        private Task _disposalTask;

        private volatile LibAvSharp.Decoder _inner;
        private volatile LibAvSharp.Decoder _innerNext;
        private volatile bool _nextFileReady;

        private bool _disposedValue;
        private ConcurrentQueue<Frame> _matQueue;
        private int _queuesize = 100;
        private volatile int _readFramesThisFile;
        private volatile int _readFramesTotal;
        private double _scale;
        private SpinLock _spinner;
        private string _inputText;

        public int RotateCount { get; set; }
        public DecoderFFMPEG(string input, double scale, bool loop)
        {
            _inputText = input;
            ResizeToDimension = false;
            _matQueue = new ConcurrentQueue<Frame>();
            _disposables = new ConcurrentBag<LibAvSharp.Decoder>();
            _readFramesTotal = 0;
            PathIndex = 0;
            FilePaths = new List<string>();
            _toLoop = loop;
            this._scale = scale;
            _nextFileReady = false;
            _spinner = new SpinLock();
            if (input != null && System.IO.File.Exists(input))
            {
                _inner = new LibAvSharp.Decoder(input, scale, TARGET_FORMAT);
                FilePaths.Add(input);
            }
            else
            {
                _inner = null;
                _innerNext = null;
            }
        }
        public DecoderFFMPEG(string input, System.Drawing.Size targetSize, bool loop)
        {
            _inputText = input;
            ResizeToDimension = true;
            _matQueue = new ConcurrentQueue<Frame>();
            _disposables = new ConcurrentBag<LibAvSharp.Decoder>();
            PathIndex = 0;
            FilePaths = new List<string>();
            _toLoop = loop;
            _readFramesTotal = 0;
            this.TargetFrameSize = targetSize;
            _nextFileReady = false;
            _spinner = new SpinLock();

            if (input != null && System.IO.File.Exists(input))
            {
                _inner = new LibAvSharp.Decoder(input, targetSize, TARGET_FORMAT);
                FilePaths.Add(input);
            }
            else
            {
                _inner = null;
                _innerNext = null;
            }
        }
        public DecoderFFMPEG(SerializationInfo info, StreamingContext context)
        {
            string text = info.GetString(nameof(_inputText));
            _inputText = text;
            _matQueue = new ConcurrentQueue<Frame>();
            _disposables = new ConcurrentBag<LibAvSharp.Decoder>();
            _scale = info.GetDouble(nameof(_scale));
            PathIndex = 0;
            _readFramesTotal = 0;
            _toLoop = info.GetBoolean(nameof(_toLoop));
            _nextFileReady = false;
            _spinner = new SpinLock();
            FilePaths = new List<string>();
            if (text[^4] == '.')
            {
                if (text != null && System.IO.File.Exists(text))
                {
                    _inner = new LibAvSharp.Decoder(text, _scale, TARGET_FORMAT);
                    FilePaths.Add(text);
                }
                else
                {
                    _inner = null;
                    _innerNext = null;
                }
            }
            else
            {
                var files = Directory.GetFiles(text);
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
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        FilePaths.Add(filtered[i]);
                    }
                    _inner = CreateDecoder();
                }
            }

            /*_inputText = input;
            ResizeToDimension = true;
            _matQueue = new ConcurrentQueue<Frame>();
            _disposables = new ConcurrentBag<LibAvSharp.Decoder>();
            PathIndex = 0;
            FilePaths = new List<string>();
            _toLoop = loop;
            this.TargetFrameSize = targetSize;
            _nextFileReady = false;
            _spinner = new SpinLock();

            if (input != null && System.IO.File.Exists(input))
            {
                _inner = new LibAvSharp.Decoder(input, targetSize, LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_NV12);
                FilePaths.Add(input);
            }
            else
            {
                _inner = null;
                _innerNext = null;
            }*/
        }

        private DecoderFFMPEG(double scale, bool loop)
            : this(null, scale, loop)
        { }

        ~DecoderFFMPEG() => Dispose(false);

        public delegate string NameParser(IFrame frame);

        public delegate DateTime TimeParser(IFrame frame);

        public NameParser CameraNameParser { get; set; }
        public string FilePath => FilePaths[PathIndex];

        public IList<string> FilePaths { get; }

        public double FramesPerSecond => GetVideoFPS();

        public bool HasMoreFrames => (PathIndex < FilePaths.Count) || _matQueue.Count > 0;

        public int PathIndex { get; protected set; }

        public bool ResizeToDimension { get; set; }

        public System.Drawing.Size TargetFrameSize { get; set; }

        public TimeParser TimeStampParser { get; set; }

        public int TotalFrameNumber
        {
            get
            {
                int val;
                bool gotLock = false;
                try
                {
                    _spinner.Enter(ref gotLock);
                    if (_inner == null)
                    {
                        val = 0;
                    }
                    else
                    {
                        val = (int)_inner.FrameCount;
                    }
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
        public static DecoderFFMPEG GetDirectoryDecoder(string folder, double scale, int rotateCount)
        {
            if (folder[^4] == '.')
            {
                var dec = new DecoderFFMPEG(folder, scale, false);
                return dec;

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
                decoder.RotateCount = rotateCount;
                for (int i = 0; i < filtered.Count; i++)
                {
                    decoder.FilePaths.Add(filtered[i]);
                }
                decoder._inner = decoder.CreateDecoder();
                Console.WriteLine(Enum.GetName(decoder._inner.PixelFormat));
                return decoder;
            }
            return null;
        }
        public static DecoderFFMPEG GetDirectoryDecoder(string folder, double scale) => GetDirectoryDecoder(folder, scale, 0);
        public static DecoderFFMPEG GetDirectoryDecoder(string folder, System.Drawing.Size targetSize)
        {
            if (folder[^4] == '.')
            {
                var dec = new DecoderFFMPEG(folder, targetSize, false);
                return dec;
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
                var decoder = new DecoderFFMPEG(folder, targetSize, false);
                for (int i = 0; i < filtered.Count; i++)
                {
                    decoder.FilePaths.Add(filtered[i]);
                }
                if (decoder._inner == null)
                {
                    decoder._inner = new LibAvSharp.Decoder(decoder.FilePath, targetSize, TARGET_FORMAT);
                }
                return decoder;
            }
            return null;
        }

        public Mat _internalGetFrame()
        {
            Mat sourceMat;
            try
            {
                sourceMat = _inner.GetNextFrame();
                if (sourceMat == null)
                {
                    if (!NextFile())
                    {
                        return null;
                    }
                    sourceMat = _inner.GetNextFrame();
                }
                _inner.GrabFrame();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("********RESET*****");
                _inner = CreateDecoder();
                _inner.Spool();

                return null;
            }

            if (sourceMat == null)
                return sourceMat;

            if (_toLoop)
            {
                if (sourceMat.Height == 0 && sourceMat.Width == 0)
                {
                    _inner = CreateDecoder();
                    _inner.Spool();
                    /*_inner.GrabFrame();
                    sourceMat = _inner.GetNextFrame();*/
                }
            }

            return sourceMat;
        }

        public void BeginReading()
        {
            _inner.Spool();
            _fileTask = new Task(() => PreloadFiles());
            _fileTask.Start();
            _queueTask = new Task(() => RunDecode());
            _queueTask.Start();
            _disposalTask = new Task(() => TrashDisposal());
            _disposalTask.Start();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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

        public Mat GetNextFrameImage()
        {
            var f = GetNextFrame();
            if (f == null)
            {
                return null;
            }
            return f.FrameData;
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DecoderFFMPEG()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(_inputText), _inputText);
            info.AddValue(nameof(_scale), _scale);
            info.AddValue(nameof(_toLoop), _toLoop);
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

        public void Wrapup()
        {
            if (_fileTask != null && !_fileTask.IsCompleted)
            {
                _fileTask.Wait();
            }
            if (_queueTask != null && !_queueTask.IsCompleted)
            {
                _queueTask.Wait();
            }
            if(_disposalTask != null && !_disposalTask.IsCompleted)
            {
                _disposalTask.Wait();
            }
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
                SourceName = FilePath,
                FrameRate = (float)GetVideoFPS(),
                FileFrameIndex = _readFramesThisFile,
                LastKeyFrame = _inner.LastKeyFrame,
                FrameIndex = ++_readFramesTotal
            };
            _readFramesThisFile++;
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
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (this._disposalTask != null)
                    {
                        if (!_disposalTask.IsCompleted)
                        {
                            _disposalTask.Wait();
                        }
                        _disposalTask.Dispose();
                        _disposalTask = null;
                    }
                    if (this._fileTask != null)
                    {
                        if (!_fileTask.IsCompleted)
                        {
                            _fileTask.Wait();
                        }
                        _fileTask.Dispose();
                        _fileTask = null;
                    }
                    if (this._queueTask != null)
                    {
                        if (!_queueTask.IsCompleted)
                        {
                            _queueTask.Wait();
                        }
                        _queueTask.Dispose();
                        _queueTask = null;
                    }
                    if (this._inner != null)
                    {
                        _inner.Dispose();
                        _inner = null;
                    }
                    if (this._innerNext != null)
                    {
                        _innerNext.Dispose();
                        _innerNext = null;
                    }
                    if (this._matQueue != null)
                    {
                        _matQueue = null;
                    }
                    if (this._disposables != null)
                    {
                        _disposables = null;
                    }
                }
                if (this._disposalTask != null)
                {
                    if (!_disposalTask.IsCompleted)
                    {
                        _disposalTask.Wait();
                    }
                    _disposalTask.Dispose();
                    _disposalTask = null;
                }
                if (this._fileTask != null)
                {
                    if (!_fileTask.IsCompleted)
                    {
                        _fileTask.Wait();
                    }
                    _fileTask.Dispose();
                    _fileTask = null;
                }
                if (this._queueTask != null)
                {
                    if (!_queueTask.IsCompleted)
                    {
                        _queueTask.Wait();
                    }
                    _queueTask.Dispose();
                    _queueTask = null;
                }
                if (this._inner != null)
                {
                    _inner.Dispose();
                    _inner = null;
                }
                if (this._innerNext != null)
                {
                    _innerNext.Dispose();
                    _innerNext = null;
                }
                if (this._matQueue != null)
                {
                    _matQueue = null;
                }
                if (this._disposables != null)
                {
                    _disposables = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        private bool NextFile()
        {
            if (PathIndex < FilePaths.Count - 1)
            {
                LibAvSharp.Decoder tmp;
                if (_fileTask == null)
                {
                    tmp = CreateDecoder();
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
                //tmp.Spool();
                LibAvSharp.Decoder tmpDecoder = null;
                try
                {
                    _spinner.Enter(ref lockGot);
                    ++PathIndex;
                    //_inner.Close();
                    tmpDecoder = (_inner);
                    _inner = tmp;
                    _readFramesThisFile = 0;
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
                if (!_nextFileReady && (TotalFrameNumber - _readFramesThisFile) < 200)
                {
                    if (PathIndex < FilePaths.Count - 1)
                    {
                        _innerNext = CreateDecoder();
                        _innerNext.Spool();
                        _nextFileReady = true;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(20);
                }
            }
        }

        private void RunDecode()
        {
            Mat image;
            bool notified = false;
            float fps = (float)GetVideoFPS();
            while (true)
            {
                if (_matQueue.Count < _queuesize)
                {
                    notified = false;
                    image = _internalGetFrame();

                    if (image == null)
                    {
                        break;
                    }
                    /*Mat image2 = new Mat(new Size(image.Height, image.Height), MatType.CV_8UC3, new Scalar(0,0,0));
                    using (Mat image2Sub = image2.SubMat(new Rect(0, 0, image.Width, image.Height)))
                    {
                        image.CopyTo(image2Sub);
                    }*/
                    Frame f = new Frame
                    {
                        SourceName = FilePath,
                        FrameRate = fps,
                        FileFrameIndex = _readFramesThisFile,
                        FrameData = image,
                        LastKeyFrame = _inner.LastKeyFrame,
                        FrameIndex = ++_readFramesTotal
                    };
                    if (TimeStampParser != null)
                    {
                        f.TimeStamp = TimeStampParser(f);
                    }
                    if (CameraNameParser != null)
                    {
                        f.CameraName = CameraNameParser(f);
                    }
                    ++_readFramesThisFile;
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
                if (_disposables.TryTake(out LibAvSharp.Decoder result))
                {
                    result.Close();
                    result.Dispose();
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private LibAvSharp.Decoder CreateDecoder()
        {
            LibAvSharp.Decoder decoder;
            if (this.ResizeToDimension)
            {
                decoder = new LibAvSharp.Decoder(FilePath, TargetFrameSize, TARGET_FORMAT);
            }
            else
            {
                decoder = new LibAvSharp.Decoder(FilePath, _scale, TARGET_FORMAT);
            }

            decoder.RotateCount = RotateCount;
            return decoder;
        }
    }
}
