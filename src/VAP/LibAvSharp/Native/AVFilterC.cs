// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    public static unsafe class AVFilterC
    {
        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avfilter_version();


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern string avfilter_configuration();


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int scalecuda_resize(AVFilterContext* filt_ctx);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avfilter_graph_create_filter(AVFilterContext** filt_ctx,
                                                              in AVFilter* filt,
                                                              in byte* name,
                                                              in byte* args,
                                                              void* opaque,
                                                              AVFilterGraph* graph_ctx);

        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern AVFilterContext* avfilter_graph_get_filter(AVFilterGraph* graph, string name);

        #region Memory Management

        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern AVFilterGraph* avfilter_graph_alloc();

        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern void avfilter_graph_free(ref AVFilterGraph* graph);



        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern AVFilterContext* avfilter_graph_alloc_filter(AVFilterGraph* graph,
                                                                          in AVFilter* filter,
                                                                          string name);

        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void avfilter_free(AVFilterContext* filter);



        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern AVFilterInOut* avfilter_inout_alloc();

        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern void avfilter_inout_free(ref AVFilterInOut* inout);

        #endregion Memory Management

        #region Links

        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avfilter_link(AVFilterContext* src, uint srcpad, AVFilterContext* dst, uint dstpad);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void avfilter_link_free(AVFilterLink** link);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void avfilter_link_free(ref AVFilterLink* link);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avfilter_link_get_channels(ref AVFilterLink* link);


        #endregion Links

        #region Pads


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avfilter_pad_count(in AVFilterPad* pads);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern string avfilter_pad_get_name(in AVFilterPad* pads, int pad_idx);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern AVMediaType avfilter_pad_get_type(in AVFilterPad* pads, int pad_idx);


        #endregion Pads

        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avfilter_config_links(AVFilterContext* filter_ctx);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
        public static extern int avfilter_process_command(AVFilterContext* filter_ctx,
                                                          in string cmd,
                                                          in string arg,
                                                          ref string res,
                                                          int res_len,
                                                          int flags);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void avfilter_register_all();


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int avfilter_register(AVFilter* filter);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern AVFilter* avfilter_get_by_name(in string name);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern AVFilter* avfilter_next(in AVFilter* prev);


        [DllImport("avfilter-8.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int avfilter_init_str(AVFilterContext* ctx, in string args);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_init_dict(AVFilterContext* graph, AVDictionary** options);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_init_dict(AVFilterContext* graph, ref AVDictionary* options);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_insert_filter(AVFilterLink* link, AVFilterContext* context, uint filt_srcpad_idx, uint filt_dstpad_idx);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern AVClass* avfilter_get_class();


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_graph_create_filter(ref AVFilterContext* graph, in AVFilter* filt, in string name, in string args, void* opaque, AVFilterGraph* graph_ctx);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern void avfilter_graph_set_auto_convert(AVFilterGraph* graph, uint flags);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_graph_config(AVFilterGraph* graph, void* log_ctx);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_graph_parse(AVFilterGraph* graph, in string filters, AVFilterInOut* inputs, AVFilterInOut* outputs, void* log_ctx);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_graph_parse2(AVFilterGraph* graph, in string filters, ref AVFilterInOut* inputs, ref AVFilterInOut* outputs);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_graph_parse_ptr(AVFilterGraph* graph, in string filters, ref AVFilterInOut* inputs, ref AVFilterInOut* outputs, void* log_ctx);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_graph_send_command(AVFilterGraph* graph, in string target, in string cmd, in string arg, ref string res, int res_len, int flags);

        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_graph_queue_command(AVFilterGraph* graph, in string target, in string cmd, in string arg, int flags, double ts);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern string avfilter_graph_dump(AVFilterGraph* graph, in string options);


        [DllImport("avfilter-8.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern int avfilter_graph_request_oldest(AVFilterGraph* graph);
    }
}
