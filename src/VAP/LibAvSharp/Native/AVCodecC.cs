﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace LibAvSharp.Native
{
    public static unsafe class AVCodecC
    {
        // avcodec_send_packet
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avcodec_send_packet([In, Out] ref AVCodecContext avctx, [In] ref AVPacket avpkt);

        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int avcodec_send_packet([In, Out] AVCodecContext* avctx, [In] AVPacket* avpkt);

        // avcodec_receive_frame
        // int avcodec_receive_frame( AVCodecContext* avctx, AVFrame* frame );
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int avcodec_receive_frame([In, Out] ref AVCodecContext avctx, [In] ref AVFrame avpkt);

        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int avcodec_receive_frame([In, Out] AVCodecContext* avctx, [In] AVFrame* avpkt);


        // av_frame_unref
        // void av_frame_unref( AVFrame* frame );
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void av_frame_unref([In, Out] ref AVFrame frame);

        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void av_frame_unref([In, Out] AVFrame* frame);


        // avcodec_get_name
        // const char *avcodec_get_name(enum AVCodecID id);
        public static string avcodec_get_name(AVCodecID codec_id)
        {
            return Marshal.PtrToStringAnsi((IntPtr)avcodec_get_name_raw(codec_id));
        }

        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "avcodec_get_name")]
        public static extern byte* avcodec_get_name_raw(AVCodecID codec_id);

        // avcodec_get_type
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern AVMediaType avcodec_get_type(AVCodecID codec_id);

        // avcodec_find_decoder_by_name
        // const AVCodec *avcodec_find_decoder_by_name(const char *name);
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern AVCodec* avcodec_find_decoder_by_name(char* name);

        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern AVCodec* avcodec_find_decoder_by_name(string name);

        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern AVCodec* avcodec_find_decoder(AVCodecID codec_id);

        // avcodec_alloc_context3
        // AVCodecContext *avcodec_alloc_context3(const AVCodec *codec);
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ref AVCodecContext avcodec_alloc_context3([In] ref AVCodec codec);
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern AVCodecContext* avcodec_alloc_context3([In] AVCodec* codec);

        // avcodec_parameters_to_context
        // int avcodec_parameters_to_context(AVCodecContext *codec, const AVCodecParameters *par);
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avcodec_parameters_to_context([In, Out] ref AVCodecContext codec, [In] ref AVCodecParameters par);
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avcodec_parameters_to_context([In, Out] AVCodecContext* codec, [In] AVCodecParameters* par);


        // avcodec_open2
        // int avcodec_open2(AVCodecContext *avctx, const AVCodec *codec, AVDictionary **options);
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avcodec_open2([In, Out] AVCodecContext* avctx, [In] AVCodec* par, AVDictionary** options);

        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int avcodec_open2([In, Out] ref AVCodecContext avctx, [In] ref AVCodec par, AVDictionary** options);


        // avcodec_free_context
        // void avcodec_free_context( AVCodecContext** avctx );
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void avcodec_free_context([In, Out] AVCodecContext** avctx);

        // avcodec_free_context
        // void avcodec_free_context( AVCodecContext** avctx );
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void avcodec_free_context([In, Out] ref AVCodecContext* avctx);


        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern AVPacket* av_packet_alloc();
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_packet_free([In, Out] ref AVPacket* packet);
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern AVPacket* av_packet_clone([In] AVPacket* packet);
        [DllImport("avcodec-59.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_packet_unref([In] AVPacket* packet);


    }
}
