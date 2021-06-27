// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVFilter
    {
        public byte* name;
        public byte* description;
        public AVFilterPad* inputs;
        public AVFilterPad* outputs;
        public AVClass* priv_class;
        public int flags;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, int> preinit;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, int> init;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, AVDictionary*, int> init_dict;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, void> uninit;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, int> query_formats;
        public int priv_size;
        public int flags_internal;
        public AVFilter* next;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, byte*, byte*, byte*, int, int, int> process_command;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, void*, int> init_opaque;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, int> activate;
    }
}
