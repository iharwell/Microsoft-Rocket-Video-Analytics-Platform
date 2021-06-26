using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibAvSharp.Native;

namespace LibAvSharp.Format
{
    unsafe public class AVStream : IDisposable
    {
        internal Native.AVStream* _stream;
        private bool disposedValue;

        public long NumberFrames => _stream->nb_frames;
        public long Duration => _stream->duration;

        internal AVStream( Native.AVStream* stream )
        {
            _stream = stream;
            disposedValue = true;
        }

        public AVRational TimeBase => _stream->time_base;
        public long StartTime => _stream->start_time;
        public AVRational AverageFrameRate => _stream->avg_frame_rate;
        public IntPtr CodecParams => (IntPtr)_stream->codecpar;

        public AVCodecID CodecID => _stream->codecpar->codec_id;
        protected virtual void Dispose( bool disposing )
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AVStream()
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
