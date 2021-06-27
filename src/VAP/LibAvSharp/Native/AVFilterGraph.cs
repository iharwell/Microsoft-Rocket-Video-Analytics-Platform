// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVFilterGraph
    {
        public AVClass* av_class;
        public AVFilterContext** filters;
        public uint nb_filters;
        public byte* scale_sws_opts;
        public int thread_type;
        public int nb_threads;
        public AVFilterGraphInternal* _internal;
        public void* opaque;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, delegate* unmanaged[Cdecl]<AVFilterContext*, void*, int, int, int>, void*, int*, int, int> execute;
        public byte* aresample_swr_opts;
        public AVFilterLink** sink_links;
        public int sink_links_count;
        public uint disable_auto_convert;
    }
}
