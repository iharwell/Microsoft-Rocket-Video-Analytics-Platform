// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibAvSharp.Codec;
using LibAvSharp.Format;
using LibAvSharp.Native;
using LibAvSharp.Util;
using OpenCvSharp;

namespace LibAvSharp
{
    public class Decoder : IDisposable
    {
        private const int maxAttempts = 512;
        private const int error_eof = -('E' | ('O' << 8) | ('F' << 16) | (' ' << 24));
        private const long AV_NOPTS_VALUE = long.MinValue;
        private const int AVERROR_EAGAIN = -11;

        private const LibAvSharp.Native.SWS_Flags ScalingMode = SWS_Flags.SWS_AREA;

        internal unsafe struct AV_CV_IMAGE
        {
            internal byte* _data;
            internal int _step;
            internal int _width;
            internal int _height;
            internal int _cn;
        }

        private string _file;
        private FormatContext _fctx;
        private CodecContext _vctx;
        private int _streamIndex;
        private LibAvSharp.Format.AVStream _stream;

        private Frame _raw_frame;
        private Frame _rgb_frame;
        private Packet _packet;


        private Task _decodeTask;
        private Task _scaleTask;
        //private AV_CV_IMAGE _image;

        private bool rawMode;
        private bool rawModeInitialized;
        private long frameNumber;
        private long picture_pts;
        private long firstFrameNumber;

        private IntPtr img_convert_ctx;
        private bool _disposedValue;

        private volatile int framesInUse;

        public long LastKeyFrame { get; private set; }

        static Decoder()
        {
            LibAvSharp.Native.AVDeviceC.avdevice_register_all();
        }

        public long FrameCount
        {
            get
            {
                long f = _stream.NumberFrames;
                if (f == 0)
                {
                    return (long)(Math.Floor(_stream.Duration * (double)_stream.TimeBase * get_fps() + 0.5));
                }
                return f;
            }
        }

        public double FPS => get_fps();

        public Decoder(string file)
            : this(file, 1.0, AVPixelFormat.AV_PIX_FMT_NONE)
        {
        }

        public Decoder(string file, double scaleFactor, AVPixelFormat format)
            : this(file, format)
        {
            ResizeToTargetDimension = false;
            TargetSize = new System.Drawing.Size((int)(_vctx.CodedWidth * scaleFactor), (int)(_vctx.CodedHeight * scaleFactor));
            ScaleFactor = scaleFactor;
        }

        public Decoder(string file, AVPixelFormat format)
        {
            framesInUse = 0;
            ResizeToTargetDimension = false;
            TargetSize = System.Drawing.Size.Empty;
            ScaleFactor = 1.0;
            _file = file;
            _fctx = new FormatContext();
            _fctx.OpenInput(file);
            _fctx.FindStreamInfo();
            _streamIndex = 0;
            _vctx = _fctx.OpenCodecContext(ref _streamIndex, AVMediaType.AVMEDIA_TYPE_VIDEO, format);
            _stream = _fctx.StreamItem(_streamIndex);
            _raw_frame = new Frame();
            _rgb_frame = new Frame();
            _packet = new Packet();
            frameNumber = 0;
            firstFrameNumber = 0;
            rawMode = false;
            rawModeInitialized = false;
            RotateCount = 0;
            RawFrames = new ConcurrentQueue<Frame>();
            ScaledFrames = new ConcurrentQueue<(Mat m, bool keyFrame)>();
            FreeFrames = new ConcurrentBag<Frame>();
        }

        public Decoder(string file, System.Drawing.Size targetSize, AVPixelFormat format)
            : this(file, format)
        {
            ResizeToTargetDimension = true;
            TargetSize = targetSize;
        }

        public System.Drawing.Size TargetSize { get; set; }
        public bool ResizeToTargetDimension { get; set; }
        public int RotateCount { get; set; }
        public AVPixelFormat PixelFormat => _vctx.PixelFormat;
        public void Close()
        {
            _stream = null;
            _vctx.Close();
            _fctx.Close();
        }

        public void Spool()
        {
            _decodeTask = new Task(() => ParallelDecodeFrames());
            _decodeTask.Start();

            _scaleTask = new Task(() => ParallelScaleFrames());
            _scaleTask.Start();
        }

        private void ParallelDecodeFrames()
        {
            long p_pts = 0;
            Frame rawFrame = MakeOrGetFrame();
            bool exit = false;
            while (!exit)
            {
                bool valid = false;
                int count_errs = 0;
                if (_fctx == null || _stream == null)
                {
                    continue;
                }

                if (_stream.NumberFrames > 0 &&
                    frameNumber > _stream.NumberFrames)
                {
                    continue;
                }
                unchecked
                {
                    p_pts = AV_NOPTS_VALUE;
                }
                while (!valid)
                {
                    _packet.Unref();
                    int ret = _fctx.ReadFrame(ref _packet);
                    if (ret == -11)
                    {
                        continue;
                    }
                    else if (ret == error_eof)
                    {
                        if (rawMode)
                            break;
                        _packet.FlushToIndex(_streamIndex);
                        exit = true;
                    }
                    else
                    {
                        AVException.ProcessException(ret);
                    }
                    if (_packet.StreamIndex != _streamIndex)
                    {
                        _packet.Unref();
                        ++count_errs;
                        if (count_errs > maxAttempts)
                        {
                            break;
                        }
                        continue;
                    }
                    if (rawMode)
                    {
                        valid = processRawPacket();
                        break;
                    }
                    ret = _vctx.SendPacket(ref _packet);
                    if (ret == (int)AVErrorCode.AV_EAGAIN || ret == (int)AVErrorCode.AV_EOF)
                    {
                        break;
                    }
                    else if (ret < 0)
                    {
                        AVException.ProcessException(ret);
                    }
                    ret = _vctx.ReceiveFrame(ref rawFrame);

                    if (ret >= 0)
                    {
                        if (p_pts == AV_NOPTS_VALUE)
                        {
                            if (_packet.Pts != AV_NOPTS_VALUE && _packet.Pts != 0)
                            {
                                p_pts = _packet.Pts;
                            }
                            else
                            {
                                p_pts = rawFrame.PacketDts;
                            }
                        }
                        valid = true;
                    }
                    else if (ret == AVERROR_EAGAIN)
                    {
                        continue;
                    }
                    else
                    {
                        ++count_errs;
                        if (count_errs > maxAttempts)
                        {
                            break;
                        }
                    }
                }
                if(exit)
                {
                    break;
                }
                if (valid)
                {
                    if (rawFrame.IsKeyFrame)
                    {
                        LastKeyFrame = frameNumber;
                    }
                    ++frameNumber;
                }
                if (!rawMode && valid && firstFrameNumber < 0)
                {
                    firstFrameNumber = dts_to_frame_number(p_pts);
                }
                if (valid)
                {
                    PostRawFrame(rawFrame);
                    rawFrame = MakeOrGetFrame();
                }

                if (count_errs > maxAttempts)
                {
                    break;
                }
            }
            return;
        }

        private int RawFramesSincePause = 0;
        private int ScaledFramesSincePause = 0;
        private void PostRawFrame(Frame f)
        {
            int limit = 8;
            if (RawFramesSincePause > 16)
            {
                while (RawFrames.Count > limit)
                {
                    Thread.Sleep(0);
                }
                RawFramesSincePause = 0;
            }
            else
            {
                ++RawFramesSincePause;
            }
            RawFrames.Enqueue(f);
        }

        private void PostScaledFrame(Mat m, bool isKeyframe)
        {
            int limit = 16;
            if (ScaledFramesSincePause > 16)
            {
                while (ScaledFrames.Count > limit)
                {
                    Thread.Sleep(1);
                }
                ScaledFramesSincePause = 0;
            }
            else
            {
                ++ScaledFramesSincePause;
            }
            ScaledFrames.Enqueue((m, isKeyframe));
        }

        public unsafe void ParallelScaleFrames()
        {
            Frame rgbFrame = MakeOrGetFrame();
            Frame rawFrame;
            while (_decodeTask.Status == TaskStatus.Running || RawFrames.Count > 0)
            {
                if (!RawFrames.TryDequeue(out rawFrame))
                {
                    Thread.Sleep(1);
                    continue;
                }
                Frame f = rawFrame;
                bool recycleRawFrame = false;
                if (rawFrame != null && rawFrame.HWFramesContextVoid != null)
                {
                    f = MakeOrGetFrame();
                    recycleRawFrame = true;
                    AVException.ProcessException(Frame.TransferHWFrame(f, rawFrame, 0), "Error copying data from GPU to CPU (av_hwframe_transfer_data)");
                }

                if (f == null || f.DataPtrEntry(0) == IntPtr.Zero)
                {
                    continue;
                }

                if (img_convert_ctx == IntPtr.Zero ||
                    TargetSize.Width != _vctx.Width ||
                    TargetSize.Height != _vctx.Height ||
                    rgbFrame.DataPtrEntry(0) == IntPtr.Zero)
                {
                    // Need to scale
                    int bufferWidth = _vctx.CodedWidth;
                    int bufferHeight = _vctx.CodedHeight;

                    if (img_convert_ctx == IntPtr.Zero)
                    {
                        img_convert_ctx = LibAvSharp.Native.SWScaleC.sws_getCachedContext(
                                            img_convert_ctx,
                                            bufferWidth, bufferHeight,
                                            f.Format,
                                            TargetSize.Width, TargetSize.Height,
                                            LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_BGR24,
                                            ScalingMode,
                                            IntPtr.Zero, IntPtr.Zero, null);
                    }

                    if (img_convert_ctx == IntPtr.Zero)
                    {
                        continue;
                    }

                    //rgbFrame.Unref();
                    rgbFrame.Format = LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_BGR24;
                    rgbFrame.Height = TargetSize.Height;
                    rgbFrame.Width = TargetSize.Width;

                    if (0 != rgbFrame.GetBuffer(64))
                    {
                        continue;
                    }
                }

                AVException.ProcessException(SWScaleC.sws_scale(img_convert_ctx,
                                                                (byte**)f.DataPtr,
                                                                (int*)f.LineSizes,
                                                                0, _vctx.CodedHeight,
                                                                (byte**)rgbFrame.DataPtr,
                                                                (int*)rgbFrame.LineSizes));

                using (var m = new Mat(TargetSize.Height, TargetSize.Width, CV_MAKETYPE(MatType.CV_8U, 3), rgbFrame.DataPtrEntry(0), rgbFrame.LineSizeItem(0)))
                {
                    var m2 = new Mat();
                    RotateFrame(m, m2);
                    PostScaledFrame(m2.Clone(), f.IsKeyFrame);
                }
                rgbFrame.Unref();
                if (recycleRawFrame)
                {
                    RecycleFrame(rawFrame);
                }
                RecycleFrame(f);
                /*RecycleFrame(rgbFrame);
                rgbFrame = MakeOrGetFrame();*/
            }
            return;
        }

        public unsafe OpenCvSharp.Mat GetNextFrame()
        {
            if ((ScaledFrames != null && ScaledFrames.Count > 0)
                || (_decodeTask != null && _decodeTask.Status == TaskStatus.Running))
            {
                (Mat m, bool isKeyframe) result;
                while (!ScaledFrames.TryDequeue(out result))
                {
                    Thread.Sleep(1);
                }
                if (result.isKeyframe)
                {
                    LastKeyFrame = frameNumber;
                }
                ++frameNumber;
                return result.m;
            }
            IntPtr data = IntPtr.Zero;
            int step = 0;
            int width = 0;
            int height = 0;
            int cn = 0;

            if (rawMode)
            {
                var m = new Mat(1, _packet.Size, CV_MAKETYPE(MatType.CV_8U, 1), (IntPtr)_packet.DataPtr, _packet.Size);
                var m2 = m.Clone();
                m.Dispose();
                return m2;
            }

            if (cvRetrieveFrame(0, ref data, ref step, ref width, ref height, ref cn))
            {
                var m = new Mat(height, width, CV_MAKETYPE(MatType.CV_8U, cn), data, step);
                var m2 = m.Clone();
                m.Dispose();
                return m2;
            }
            return null;
        }

        public bool GrabFrame()
        {
            if (this._decodeTask != null)
            {
                return _decodeTask.IsFaulted;
            }

            bool valid = false;
            int count_errs = 0;
            if (_fctx == null || _stream == null)
            {
                return false;
            }

            if (_stream.NumberFrames > 0 &&
                frameNumber > _stream.NumberFrames)
            {
                return false;
            }
            unchecked
            {
                picture_pts = AV_NOPTS_VALUE;
            }
            while (!valid)
            {
                _packet.Unref();
                int ret = _fctx.ReadFrame(ref _packet);
                if (ret == -11)
                {
                    continue;
                }
                else if (ret == error_eof)
                {
                    if (rawMode)
                        break;
                    _packet.FlushToIndex(_streamIndex);
                }
                else
                {
                    AVException.ProcessException(ret);
                }
                if (_packet.StreamIndex != _streamIndex)
                {
                    _packet.Unref();
                    ++count_errs;
                    if (count_errs > maxAttempts)
                    {
                        break;
                    }
                    continue;
                }
                if (rawMode)
                {
                    valid = processRawPacket();
                    break;
                }
                ret = _vctx.SendPacket(ref _packet);
                if (ret == (int)AVErrorCode.AV_EAGAIN || ret == (int)AVErrorCode.AV_EOF)
                {
                    break;
                }
                else if (ret < 0)
                {
                    AVException.ProcessException(ret);
                }
                ret = _vctx.ReceiveFrame(ref _raw_frame);

                if (ret >= 0)
                {
                    if (picture_pts == AV_NOPTS_VALUE)
                    {
                        if (_packet.Pts != AV_NOPTS_VALUE && _packet.Pts != 0)
                        {
                            picture_pts = _packet.Pts;
                        }
                        else
                        {
                            picture_pts = _raw_frame.PacketDts;
                        }
                    }
                    valid = true;
                }
                else if (ret == AVERROR_EAGAIN)
                {
                    continue;
                }
                else
                {
                    ++count_errs;
                    if (count_errs > maxAttempts)
                    {
                        break;
                    }
                }
            }

            if (valid)
            {
                if (_raw_frame.IsKeyFrame)
                {
                    LastKeyFrame = frameNumber;
                }
                ++frameNumber;
            }
            if (!rawMode && valid && firstFrameNumber < 0)
            {
                firstFrameNumber = dts_to_frame_number(picture_pts);
            }
            return valid;
        }

        private bool processRawPacket()
        {
            if (_packet.Data == IntPtr.Zero) //EOF
            {
                return false;
            }
            if (!rawModeInitialized)
            {
                rawModeInitialized = true;
                //var evideoCodec = _stream.CodecID;
                //string filterName = null;
            }
            return true;
        }

        private long dts_to_frame_number(long dts)
        {
            double sec = dts_to_sec(dts);
            return (long)(get_fps() * sec + 0.5);
        }

        private double get_fps()
        {
            double fps = (double)_stream.AverageFrameRate;
            if (fps < double.Epsilon)
            {
                fps = (double)_stream.AverageFrameRate;
            }
            if (fps < double.Epsilon)
            {
                fps = 1.0 / (double)(_vctx.TimeBase);
            }
            return fps;
        }

        private double dts_to_sec(long dts)
        {
            return (double)(dts - _stream.StartTime) * ((double)_stream.TimeBase);
        }

        private void RotateFrame(Mat src, Mat dst)
        {
            if (RotateCount > 0)
            {
                Cv2.Rotate(src, dst, (RotateFlags)(RotateCount - 1));
            }
            else
            {
                src.CopyTo(dst);
            }
        }

        private double _scaleFactor;

        public double ScaleFactor
        {
            get
            {
                if (ResizeToTargetDimension)
                {
                    if (_vctx.CodedHeight * 1.0f / TargetSize.Height < _vctx.CodedWidth * 1.0f / TargetSize.Width)
                    {
                        // Scale height to size;
                        return TargetSize.Height * 1.0 / _vctx.CodedHeight;
                    }
                    else
                    {
                        return TargetSize.Width * 1.0 / _vctx.CodedWidth;
                    }
                }
                else
                {
                    return _scaleFactor;
                }
            }
            set
            {
                _scaleFactor = value;
            }
        }

        private unsafe bool cvRetrieveFrame(int n, ref IntPtr data, ref int step, ref int width, ref int height, ref int cn)
        {
            if (rawMode)
            {
                data = _packet.Data;
                step = _packet.Size;
                width = _packet.Size;
                height = 1;
                cn = 1;
                return _packet.Data != IntPtr.Zero;
            }

            Frame f = _raw_frame;

            if (_raw_frame != null && _raw_frame.HWFramesContextVoid != null)
            {
                f = new Frame();
                AVException.ProcessException(Frame.TransferHWFrame(f, _raw_frame, 0), "Error copying data from GPU to CPU (av_hwframe_transfer_data)");
            }

            if (f == null || f.DataPtrEntry(0) == IntPtr.Zero)
            {
                return false;
            }

            if (img_convert_ctx == IntPtr.Zero ||
                TargetSize.Width != _vctx.Width ||
                TargetSize.Height != _vctx.Height ||
                _rgb_frame.DataPtrEntry(0) == IntPtr.Zero)
            {
                // Need to scale
                int bufferWidth = _vctx.CodedWidth;
                int bufferHeight = _vctx.CodedHeight;

                if (img_convert_ctx == IntPtr.Zero)
                {
                    img_convert_ctx = LibAvSharp.Native.SWScaleC.sws_getCachedContext(
                                        img_convert_ctx,
                                        bufferWidth, bufferHeight,
                                        f.Format,
                                        TargetSize.Width, TargetSize.Height,
                                        LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_BGR24,
                                        ScalingMode,
                                        IntPtr.Zero, IntPtr.Zero, null);
                }

                if (img_convert_ctx == IntPtr.Zero)
                {
                    return false;
                }

                _rgb_frame.Unref();
                _rgb_frame.Format = LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_BGR24;
                _rgb_frame.Height = TargetSize.Height;
                _rgb_frame.Width = TargetSize.Width;

                if (0 != _rgb_frame.GetBuffer(64))
                {
                    return false;
                }
            }

            AVException.ProcessException(SWScaleC.sws_scale(img_convert_ctx,
                                                            (byte**)f.DataPtr,
                                                            (int*)f.LineSizes,
                                                            0, _vctx.CodedHeight,
                                                            (byte**)_rgb_frame.DataPtr,
                                                            (int*)_rgb_frame.LineSizes));

            data = _rgb_frame.DataPtrEntry(0);
            step = _rgb_frame.LineSizeItem(0);
            width = TargetSize.Width;
            height = TargetSize.Height;
            cn = 3;

            if (f != _raw_frame)
            {
                f.Unref();
            }
            return true;
        }

        private ConcurrentQueue<Frame> RawFrames;
        private ConcurrentQueue<(Mat m, bool keyFrame)> ScaledFrames;

        private ConcurrentBag<Frame> FreeFrames;

        private Frame MakeOrGetFrame()
        {
            if(FreeFrames.TryTake(out Frame f))
            {
                return f;
            }
            else
            {
                if(framesInUse < 50)
                {
                    framesInUse++;
                    return new Frame();
                }
                else
                {
                    while (!FreeFrames.TryTake(out f))
                    {
                        Thread.Sleep(1);
                    }
                    return f;
                }
            }
        }
        private void RecycleFrame(Frame f)
        {
            f.Unref();
            FreeFrames.Add(f);
        }

        private static int CV_MAKETYPE(OpenCvSharp.MatType depth, int cn)
        {
            return (CV_MAT_DEPTH(depth) + (((cn) - 1) << CV_CN_SHIFT));
        }

        private const int CV_CN_SHIFT = 3;

        private static int CV_MAT_DEPTH(int flags)
        {
            return flags & CV_MAT_DEPTH_MASK;
        }

        private const int CV_MAT_DEPTH_MASK = CV_DEPTH_MAX - 1;
        private const int CV_DEPTH_MAX = 1 << CV_CN_SHIFT;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (img_convert_ctx != IntPtr.Zero)
                {
                    SWScaleC.sws_freeContext(img_convert_ctx);
                    img_convert_ctx = IntPtr.Zero;
                    this._decodeTask.Dispose();
                    this._scaleTask.Dispose();
                }
                while(RawFrames.Count>0)
                {
                    if(RawFrames.TryDequeue(out var f))
                    {
                        f.Unref();
                        f.Dispose();
                    }
                }
                while (FreeFrames.Count > 0)
                {
                    if (FreeFrames.TryTake(out var f))
                    {
                        f.Unref();
                        f.Dispose();
                    }
                }
                while (ScaledFrames.Count > 0)
                {
                    if (ScaledFrames.TryDequeue(out var result))
                    {
                        result.m.Dispose();
                    }
                }
                if (disposing)
                {
                    if (_stream != null)
                    {
                        this._stream.Dispose();
                        _stream = null;
                    }
                    if (_rgb_frame != null)
                    {
                        this._rgb_frame.Dispose();
                        _rgb_frame = null;
                    }
                    if (_raw_frame != null)
                    {
                        this._raw_frame.Dispose();
                        _raw_frame = null;
                    }
                    if (_packet != null)
                    {
                        this._packet.Dispose();
                        _packet = null;
                    }
                    if (_fctx != null)
                    {
                        this._fctx.Dispose();
                        _fctx = null;
                    }
                    if (_vctx != null)
                    {
                        this._vctx.Dispose();
                        _vctx = null;
                    }
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Decoder()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
