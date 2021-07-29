// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibAvSharp.Native
{
    public static unsafe class AVFormatC
    {
        // AVStream
        // AVInputFormat
        // AVOutputFormat
        // AVFormatContext
        // av_find_best_stream
        // avformat_open_input
        // avformat_find_stream_info
        // av_dump_format
        // av_read_frame
        // avformat_close_input
        // avcodec_parameters_to_context

        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int av_find_best_stream([In, Out] ref AVFormatContext ic,
                                                        AVMediaType type,
                                                        int wanted_stream_nb,
                                                        int related_stream,
                                                        [In] IntPtr decoder_ret,
                                                        int flags);

        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int av_find_best_stream([In, Out] AVFormatContext* ic,
                                                        AVMediaType type,
                                                        int wanted_stream_nb,
                                                        int related_stream,
                                                        [In] AVCodec** decoder_ret,
                                                        int flags);

        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int av_find_best_stream([In, Out] AVFormatContext* ic,
                                                        AVMediaType type,
                                                        int wanted_stream_nb,
                                                        int related_stream,
                                                        [In] IntPtr decoder_ret,
                                                        int flags);



        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int avformat_close_input([In, Out] AVFormatContext** ic);

        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int avformat_close_input([In, Out] ref AVFormatContext* ic);



        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int avformat_open_input([In, Out] AVFormatContext** ic, [In] byte* url, [In] ref AVInputFormat fmt, [In, Out] AVDictionary** options);

        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int avformat_open_input([In, Out] ref AVFormatContext* ic, [In] string url, [In] ref AVInputFormat fmt, [In, Out] ref AVDictionary* options);

        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int avformat_open_input([In, Out] ref AVFormatContext* ic, [In] string url, [In] AVInputFormat* fmt, [In, Out] AVDictionary** options);



        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int avformat_find_stream_info([In, Out] AVFormatContext* ic, [In, Out] AVDictionary** options);
        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int avformat_find_stream_info([In, Out] ref AVFormatContext ic, [In, Out] ref AVDictionary* options);


        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int av_read_frame([In, Out] AVFormatContext* ic, [In, Out] AVPacket* options);


        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int av_dump_format([In, Out] AVFormatContext* ic, int index, string url, int is_output);

    }
}
