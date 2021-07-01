// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LibAvSharp.Native;

namespace LibAvSharp.Filter
{
    public unsafe class FilterInOut : IDisposable
    {
        internal AVFilterInOut* _filterIO;
        private bool _disposedValue;

        public FilterInOut()
        {
            _filterIO = AVFilterC.avfilter_inout_alloc();
        }
        public FilterInOut(AVFilterInOut* ptr)
        {
            _filterIO = ptr;
            _disposedValue = true;
        }

        public string Name
        {
            get
            {
                return Marshal.PtrToStringAnsi((IntPtr)_filterIO->name);
            }
            set
            {
                // need to copy the name into unmanaged memory.
                if (_filterIO->name != null)
                {
                    AVUtilsC.av_freep(&_filterIO->name);
                }
                if (value != null)
                {
                    _filterIO->name = AVUtilsC.av_strdup(value);
                }
            }
        }

        public FilterContext Context
        {
            get
            {
                if (_filterIO->filter_ctx == null)
                {
                    return null;
                }
                else
                {
                    return new FilterContext(_filterIO->filter_ctx);
                }
            }
            set
            {
                if (value == null)
                {
                    _filterIO->filter_ctx = null;
                }
                else
                {
                    _filterIO->filter_ctx = value._context;
                }
            }
        }

        public int PadIndex
        {
            get => _filterIO->pad_idx;
            set => _filterIO->pad_idx = value;
        }

        public FilterInOut Next
        {
            get
            {
                if( _filterIO->next == null )
                {
                    return null;
                }
                return new FilterInOut(_filterIO->next);
            }
            set
            {
                if( value == null)
                {
                    _filterIO->next = null;
                }
                else
                {
                    _filterIO->next = value._filterIO;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                if (_filterIO != null)
                {
                    AVFilterC.avfilter_inout_free(ref _filterIO);
                }
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FilterInOut()
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
