using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct AVCodec
    {
        public byte* name;
        public byte* long_name;
        public AVMediaType type;
        public AVCodecID id;
        public int capabilities;
        public AVRational* supported_framerates;
        public AVPixelFormat* pix_fmts;
        public int* supported_samplerates;
        public AVSampleFormat* sample_fmts;
        public ulong* channel_layouts;
        public byte max_lowres;
        public AVClass* priv_class;
        public AVProfile* profiles;

        public byte* wrapper_name;
        public int priv_data_size;

        // int (*update_thread_context)(struct AVCodecContext *dst, const struct AVCodecContext *src);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, AVCodecContext*> update_thread_context;
        // int (*update_thread_context_for_user)(struct AVCodecContext *dst, const struct AVCodecContext *src);
        public delegate* unmanaged[Cdecl]<AVCodecContext *, AVCodecContext *> update_thread_context_for_user;

        // AVCodecDefault*
        public AVCodecDefault* defaults;

        // void (*init_static_data)(struct AVCodec *codec);
        public delegate* unmanaged[Cdecl]<AVCodec *, void> init_static_data;

        // int (*init)(struct AVCodecContext *);
        public delegate* unmanaged[Cdecl]<AVCodecContext *, int> init;

        // int (*encode_sub)(struct AVCodecContext *, uint8_t *buf, int buf_size, const struct AVSubtitle *sub);
        public delegate* unmanaged[Cdecl]<AVCodecContext *, byte*, int, AVSubtitle *, int> encode_sub;

        // int (* encode2) (struct AVCodecContext *avctx, struct AVPacket *avpkt, const struct AVFrame *frame, int *got_packet_ptr);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, AVPacket*, AVFrame*, int*, int> encode2;

        // int (* decode) (struct AVCodecContext *avctx, void *outdata, int *got_frame_ptr, struct AVPacket *avpkt);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, void*, int*, AVPacket*, int> decode;

        // int (*close)(struct AVCodecContext *);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, int> close;

        // int (*receive_packet)(struct AVCodecContext *avctx, struct AVPacket *avpkt);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, AVPacket*, int> receive_packet;

        // int (* receive_frame) (struct AVCodecContext *avctx, struct AVFrame *frame);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, AVFrame*, int> receive_frame;

        // void (*flush)(struct AVCodecContext *);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, void> flush;

        public int caps_public;

        public byte* bsfs;

        public void** hw_configs;

        public uint* codec_tags;

    }
}
