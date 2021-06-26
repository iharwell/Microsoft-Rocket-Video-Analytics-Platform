using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.IO
{
    unsafe public class IOContext : IDisposable
    {
        private Native.AVIOContext* _context;
        private byte* buffer;
        private ulong size;

        private BufferData bd;

        private byte* avio_buffer;
        private ulong avio_buffer_size;
        private bool disposedValue;

        protected int BufferSize { get; private set; }

        protected struct BufferData
        {
            public byte* buffer;
            public ulong size;
        }

        public IOContext(int bufferSize)
        {
            buffer = null;
            size = 0;
            avio_buffer_size = (ulong)bufferSize;
            avio_buffer = Native.AVUtilsC.av_malloc((ulong)bufferSize);
        }

        static int read_packet(void* opaque, byte* buf, int buf_size)
        {
            var bd = (BufferData*)opaque;
            buf_size = (int)Math.Min((decimal)buf_size, (decimal)(bd->size));
            Buffer.MemoryCopy(bd->buffer, buf, (long)(bd->size), buf_size);
            bd->buffer += buf_size;
            bd->size -= (ulong)buf_size;
            return buf_size;
        }

        public void MapFile( string url )
        {
            Native.AVUtilsC.av_file_map(url, out buffer, out size, 0, null);
            bd.buffer = buffer;
            bd.size = size;
        }

        protected virtual void Dispose( bool disposing )
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if(avio_buffer != null )
                {
                    Native.AVUtilsC.av_freep(ref avio_buffer);
                }
                Native.AVUtilsC.av_freep(&(_context->buffer));
                if( buffer != null )
                {
                    Native.AVUtilsC.av_file_unmap(buffer, size);
                    buffer = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~IOContext()
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
