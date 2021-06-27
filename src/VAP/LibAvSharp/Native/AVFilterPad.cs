// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVFilterPad
    {
        public byte* name;
        public AVMediaType type;
        public delegate* unmanaged[Cdecl]<AVFilterLink*, int, int, AVFrame*> get_video_buffer;
        public delegate* unmanaged[Cdecl]<AVFilterLink*, int, AVFrame*> get_audio_buffer;
        public delegate* unmanaged[Cdecl]<AVFilterLink*, AVFrame*, int> filter_frame;
        public delegate* unmanaged[Cdecl]<AVFilterLink*, int> poll_frame;
        public delegate* unmanaged[Cdecl]<AVFilterLink*, int> request_frame;
        public delegate* unmanaged[Cdecl]<AVFilterLink*, int> config_props;
        public int needs_fifo;
        public int needs_writable;


    }
}
