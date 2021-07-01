using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibAvSharp.Native;

namespace LibAvSharp.Util
{
    unsafe public class Frame : IDisposable
    {
        internal AVFrame* _frame;
        private bool disposedValue;

        public Frame()
        {
            _frame = AVUtilsC.av_frame_alloc();
        }

        public long PacketDts => _frame->pkt_dts;

        public int Width
        {
            get => _frame->width;
            set => _frame->width = value;
        }

        public int Height
        {
            get => _frame->height;
            set => _frame->height = value;
        }

        public AVPixelFormat Format
        {
            get => (AVPixelFormat)_frame->format;
            set => _frame->format = (int)value;
        }
        public IntPtr LineSizes
        {
            get => (IntPtr)(&_frame->linesize0);
        }
        public long TimeStamp
        {
            get
            {
                return _frame->pts;
            }
            set
            {
                _frame->pts = value;
            }
        }
        public long BestEffortTimeStamp
        {
            get
            {
                return _frame->best_effort_timestamp;
            }
        }
        public int LineSizeItem( int index )
        {
            return _frame->linesize(index);
        }

        public IntPtr HWFramesContext => (IntPtr)_frame->hw_frames_ctx;
        public unsafe void* HWFramesContextVoid => (void*)_frame->hw_frames_ctx;

        public IntPtr DataPtr => (IntPtr)(&_frame->data0);

        public void Unref() => AVUtilsC.av_frame_unref(_frame);

        public IntPtr DataPtrEntry(int index)
        {
            if(index>=4 )
            {
                throw new IndexOutOfRangeException();
            }
            return (IntPtr)((&_frame->data0)[index]);
        }

        public static int TransferHWFrame( Frame destination, Frame source, int flags )
        {
            return AVUtilsC.av_hwcontext_transfer_data(destination._frame, source._frame, flags);
        }

        public int GetBuffer( int align )
        {
            return AVUtilsC.av_frame_get_buffer(_frame, align);
        }

        protected virtual void Dispose( bool disposing )
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                AVUtilsC.av_frame_free(ref _frame);
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Frame()
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
