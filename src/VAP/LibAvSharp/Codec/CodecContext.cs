using System;
using LibAvSharp.Native;
using LibAvSharp.Util;

namespace LibAvSharp.Codec
{
    unsafe public class CodecContext : IDisposable
    {
        internal AVCodecContext* _native_context;
        private bool disposedValue;

        public int Width => _native_context->width;
        public int Height => _native_context->height;
        public int CodedWidth => _native_context->coded_width;
        public int CodedHeight => _native_context->coded_height;
        public AVRational SampleAspectRatio => _native_context->sample_aspect_ratio;
        public AVPixelFormat PixelFormat
        {
            get
            {
                return _native_context->pix_fmt;
            }
            set
            {
                _native_context->pix_fmt = value;
            }
        }

        private Packet _currentPacket;

        public AVRational TimeBase => _native_context->time_base;

        public int SendPacket( ref Packet pkt )
        {
            int ret = AVCodecC.avcodec_send_packet(_native_context, pkt._packet);

            _currentPacket = pkt;
            return ret;
        }
        public int ReceiveFrame(ref Frame output)
        {
            int outVal;
            outVal = AVCodecC.avcodec_receive_frame(_native_context, output._frame);

            if (outVal < 0 && outVal != -11 )
            {

            }
            if (outVal >= 0 && _currentPacket!=null)
            {
                _currentPacket._packet->size -= outVal;
                _currentPacket._packet->data += outVal;
            }

            return outVal;
        }

        public void Close()
        {
            AVCodecC.avcodec_free_context(ref _native_context);
            _native_context = null;
        }

        internal CodecContext( AVCodecContext* native_context )
        {
            _native_context = native_context;
        }

        protected virtual void Dispose( bool disposing )
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                if (_native_context != null)
                {
                    AVCodecC.avcodec_free_context(ref _native_context);
                }
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~CodecContext()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
