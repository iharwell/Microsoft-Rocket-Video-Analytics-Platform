using System;
using LibAvSharp.Codec;
using LibAvSharp.Format;
using LibAvSharp.Native;
using LibAvSharp.Util;
using OpenCvSharp;

namespace Decoder2
{
    public class Decoder : IDisposable
    {
        private const int maxAttempts = 512;
        private const int error_eof = -('E' | ('O' << 8) | ('F' << 16) | (' ' << 24));
        private const long AV_NOPTS_VALUE = long.MinValue;
        private const int AVERROR_EAGAIN = -11;

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

        private Frame _picture;
        private Frame _rgb_frame;
        private Packet _packet;

        private AV_CV_IMAGE _image;

        private bool rawMode;
        private bool rawModeInitialized;
        private long frameNumber;
        private long picture_pts;
        private long firstFrameNumber;

        private IntPtr img_convert_ctx;
        private bool _disposedValue;

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
        {
            _file = file;
            _fctx = new FormatContext();
            _fctx.OpenInput(file);
            _fctx.FindStreamInfo();
            _streamIndex = 0;
            _vctx = _fctx.OpenCodecContext(ref _streamIndex, AVMediaType.AVMEDIA_TYPE_VIDEO, format);
            _stream = _fctx.StreamItem(_streamIndex);
            _picture = new Frame();
            _rgb_frame = new Frame();
            _packet = new Packet();
            frameNumber = 0;
            firstFrameNumber = 0;
            ScaleFactor = scaleFactor;
            rawMode = false;
            rawModeInitialized = false;
        }

        public void Close()
        {
            _stream = null;
            _vctx.Close();
            _fctx.Close();
        }

        public unsafe OpenCvSharp.Mat GetNextFrame()
        {
            IntPtr data = IntPtr.Zero;
            int step = 0;
            int width = 0;
            int height = 0;
            int cn = 0;

            if (rawMode)
            {
                return new Mat(1, _packet.Size, CV_MAKETYPE(MatType.CV_8U, 1), (IntPtr)_packet.DataPtr, _packet.Size);
            }

            if (cvRetrieveFrame(0, ref data, ref step, ref width, ref height, ref cn))
            {
                return new Mat(height, width, CV_MAKETYPE(MatType.CV_8U, cn), data, step);
            }
            return null;
        }

        public bool GrabFrame()
        {
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
                if (ret == error_eof)
                {
                    if (rawMode)
                        break;
                    _packet.FlushToIndex(_streamIndex);
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
                if (_vctx.SendPacket(ref _packet) < 0)
                {
                    break;
                }
                ret = _vctx.ReceiveFrame(ref _picture);

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
                            picture_pts = _picture.PacketDts;
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

        private void RotateFrame(Mat tmp)
        {
            throw new NotImplementedException();
        }

        public double ScaleFactor { get; set; }

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

            Frame f = _picture;

            if (_picture != null && _picture.HWFramesContextVoid != null)
            {
                f = new Frame();
                if (Frame.TransferHWFrame(f, _picture, 0) < 0)
                {
                    throw new OpenCVException("Error copying data from GPU to CPU (av_hwframe_transfer_data)");
                    return false;
                }
            }

            if (f == null || f.DataPtrEntry(0) == IntPtr.Zero)
            {
                return false;
            }

            if (img_convert_ctx == IntPtr.Zero ||
                _image._width != _vctx.Width ||
                _image._height != _vctx.Height ||
                _image._data == null)
            {
                // Need to scale
                int bufferWidth = _vctx.CodedWidth;
                int bufferHeight = _vctx.CodedHeight;

                img_convert_ctx = LibAvSharp.Native.SWScaleC.sws_getCachedContext(
                                    img_convert_ctx,
                                    bufferWidth, bufferHeight,
                                    f.Format,
                                    (int)(bufferWidth * ScaleFactor), (int)(bufferHeight * ScaleFactor),
                                    LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_BGR24,
                                    LibAvSharp.Native.SWS_Flags.SWS_FAST_BILINEAR,
                                    IntPtr.Zero, IntPtr.Zero, null);

                if (img_convert_ctx == IntPtr.Zero)
                {
                    return false;
                }

                _rgb_frame.Unref();
                _rgb_frame.Format = LibAvSharp.Native.AVPixelFormat.AV_PIX_FMT_BGR24;
                _rgb_frame.Height = (int)(bufferHeight * ScaleFactor);
                _rgb_frame.Width = (int)(bufferWidth * ScaleFactor);

                if (0 != _rgb_frame.GetBuffer(64))
                {
                    return false;
                }

                _image._width = (int)(bufferWidth * ScaleFactor);
                _image._height = (int)(bufferHeight * ScaleFactor);
                _image._cn = 3;
                _image._data = (byte*)_rgb_frame.DataPtrEntry(0);
                _image._step = _rgb_frame.LineSizeItem(0);
            }

            LibAvSharp.Native.SWScaleC.sws_scale(img_convert_ctx,
                                                 (byte**)f.DataPtr,
                                                 (int*)f.LineSizes,
                                                 0, _vctx.CodedHeight,
                                                 (byte**)_rgb_frame.DataPtr,
                                                 (int*)_rgb_frame.LineSizes);
            data = (IntPtr)_image._data;
            step = _image._step;
            width = _image._width;
            height = _image._height;
            cn = _image._cn;

            if (f != _picture)
            {
                f.Unref();
            }
            return true;
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
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                if (_stream != null)
                {
                    this._stream.Dispose();
                }
                this._rgb_frame.Dispose();
                this._picture.Dispose();
                this._packet.Dispose();
                this._vctx.Dispose();
                this._fctx.Dispose();

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
