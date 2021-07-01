// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVHWAccel
    {
        public char* name;
        public AVMediaType type;
        public AVCodecID id;
        public AVPixelFormat pix_fmt;
        public int capabilities;
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE1006 // Naming Styles
        private delegate* unmanaged[Cdecl]<AVCodecContext*, AVFrame*, int> alloc_frame;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, int, byte*, uint, int> decode_params;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, byte*, uint, int> decode_slice;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, int> end_frame;
        private int frame_priv_data_size;
        private delegate* unmanaged[Cdecl]<void*, int> decode_mb;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, int> init;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, int> uninit;
        private int priv_data_size;
        private int caps_internal;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, AVBufferRef*, int> frame_params;
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
    }
}
