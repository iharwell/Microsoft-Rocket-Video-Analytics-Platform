// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using LibAvSharp.Native;

namespace LibAvSharp.Util
{
    public unsafe class Packet : IDisposable
    {
        internal AVPacket* _packet;
        private bool disposedValue;

        public Packet()
        {
            _packet = AVCodecC.av_packet_alloc();
        }

        /*public void NotifyBytesRead( int bytesRead )
        {
            _packet->size -= bytesRead;
            _packet->data += bytesRead;
        }*/
        public void Unref()
        {
            AVCodecC.av_packet_unref(_packet);
        }

        public int Size => _packet->size;
        public IntPtr Data => (IntPtr)_packet->data;
        public IntPtr DataPtr => (IntPtr)(&_packet->data);

        public long Pts => _packet->pts;

        public void FlushToIndex(int streamIndex)
        {
            _packet->data = null;
            _packet->size = 0;
            _packet->stream_index = streamIndex;
        }

        public int StreamIndex => _packet->stream_index;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                AVCodecC.av_packet_free(ref _packet);

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Packet()
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