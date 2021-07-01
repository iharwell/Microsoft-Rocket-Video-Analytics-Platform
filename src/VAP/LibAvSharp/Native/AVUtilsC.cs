// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibAvSharp.Native
{
    public enum AVMediaType : int
    {
        AVMEDIA_TYPE_UNKNOWN = -1,  //< Usually treated as AVMEDIA_TYPE_DATA
        AVMEDIA_TYPE_VIDEO,
        AVMEDIA_TYPE_AUDIO,
        AVMEDIA_TYPE_DATA,          //< Opaque data information usually continuous
        AVMEDIA_TYPE_SUBTITLE,
        AVMEDIA_TYPE_ATTACHMENT,    //< Opaque data information usually sparse
        AVMEDIA_TYPE_NB
    }
    public enum AVPictureType : int
    {
        AV_PICTURE_TYPE_NONE = 0, //< Undefined
        AV_PICTURE_TYPE_I,        //< Intra
        AV_PICTURE_TYPE_P,        //< Predicted
        AV_PICTURE_TYPE_B,        //< Bi-dir predicted
        AV_PICTURE_TYPE_S,        //< S(GMC)-VOP MPEG-4
        AV_PICTURE_TYPE_SI,       //< Switching Intra
        AV_PICTURE_TYPE_SP,       //< Switching Predicted
        AV_PICTURE_TYPE_BI,       //< BI type
    }
    public static unsafe class AVUtilsC
    {
        public const long AV_NOPTS_VALUE = -(0x7FFFFFFFFFFFFFFF);

        // AVMEDIA_TYPE_VIDEO
        // AVMEDIA_TYPE_AUDIO
        // AV_NOPTS_VALUE

        // av_get_media_type_string
        // const char *av_get_media_type_string(enum AVMediaType media_type);
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* av_get_media_type_string(AVMediaType media_type);

        //char av_get_picture_type_char(enum AVPictureType pict_type);
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte av_get_picture_type_char(AVMediaType media_type);

        //char av_get_picture_type_char(enum AVPictureType pict_type);
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* av_malloc(ulong size);

        //char av_get_picture_type_char(enum AVPictureType pict_type);
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_free(void* ptr);

        //char av_get_picture_type_char(enum AVPictureType pict_type);
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_freep(void** ptr);
        //char av_get_picture_type_char(enum AVPictureType pict_type);
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_freep([In, Out] ref void* ptr);

        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_freep([In, Out] byte** ptr);

        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_freep([In, Out] ref byte* ptr);

        //char av_get_picture_type_char(enum AVPictureType pict_type);
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern AVFrame* av_frame_alloc();

        //void av_frame_unref(AVFrame *frame);
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_frame_unref([In, Out] AVFrame* frame);

        //char av_get_picture_type_char(enum AVPictureType pict_type);
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_frame_free([In, Out] ref AVFrame* frame);

        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void av_packet_unref(AVPacket* pkt);

        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int av_hwcontext_transfer_data([Out, In] AVFrame* dst, [In] AVFrame* src, int flags);

        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int av_hwcontext_transfer_data([Out, In] ref AVFrame dst, [In] ref AVFrame src, int flags);

        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int av_frame_get_buffer([Out, In] AVFrame* frame, int align);


        // int av_file_map(const char *filename, uint8_t **bufptr, size_t *size, int log_offset, void *log_ctx);\
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int av_file_map([In] string filename, [Out] out byte* bufptr, out ulong size, int log_offset, void* log_ctx);

        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int av_file_unmap(byte* bufptr, ulong size);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern byte* av_strdup(string str);
    }
}
