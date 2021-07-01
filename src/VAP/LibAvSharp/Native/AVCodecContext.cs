// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace LibAvSharp.Native
{
    [Flags]
    public enum AVCodecContextFlags : uint
    {
        AV_CODEC_FLAG_UNALIGNED = (1 << 0),
        AV_CODEC_FLAG_QSCALE = (1 << 1),
        AV_CODEC_FLAG_4MV = (1 << 2),
        AV_CODEC_FLAG_OUTPUT_CORRUPT = (1 << 3),
        AV_CODEC_FLAG_QPEL = (1 << 4),
        AV_CODEC_FLAG_DROPCHANGED = (1 << 5),
        AV_CODEC_FLAG_PASS1 = (1 << 9),
        AV_CODEC_FLAG_PASS2 = (1 << 10),
        AV_CODEC_FLAG_LOOP_FILTER = (1 << 11),
        AV_CODEC_FLAG_GRAY = (1 << 13),
        AV_CODEC_FLAG_PSNR = (1 << 15),
        AV_CODEC_FLAG_TRUNCATED = (1 << 16),
        AV_CODEC_FLAG_INTERLACED_DCT = (1 << 18),
        AV_CODEC_FLAG_LOW_DELAY = (1 << 19),
        AV_CODEC_FLAG_GLOBAL_HEADER = (1 << 22),
        AV_CODEC_FLAG_BITEXACT = (1 << 23),
        AV_CODEC_FLAG_AC_PRED = (1 << 24),
        AV_CODEC_FLAG_INTERLACED_ME = (1 << 29),
        AV_CODEC_FLAG_CLOSED_GOP = (1U << 31)
    }
    [Flags]
    public enum AVCodecContextFlags2 : uint
    {
        AV_CODEC_FLAG2_FAST = (1 << 0),
        AV_CODEC_FLAG2_NO_OUTPUT = (1 << 2),
        AV_CODEC_FLAG2_LOCAL_HEADER = (1 << 3),
        AV_CODEC_FLAG2_DROP_FRAME_TIMECODE = (1 << 13),
        AV_CODEC_FLAG2_CHUNKS = (1 << 15),
        AV_CODEC_FLAG2_IGNORE_CROP = (1 << 16),
        AV_CODEC_FLAG2_SHOW_ALL = (1 << 22),
        AV_CODEC_FLAG2_EXPORT_MVS = (1 << 28),
        AV_CODEC_FLAG2_SKIP_MANUAL = (1 << 29),
        AV_CODEC_FLAG2_RO_FLUSH_NOOP = (1 << 30),
    }
    [Flags]
    public enum AVCodecContextExportSideDataFlags : uint
    {
        AV_CODEC_EXPORT_DATA_MVS = (1 << 0),
        AV_CODEC_EXPORT_DATA_PRFT = (1 << 1),
        AV_CODEC_EXPORT_DATA_VIDEO_ENC_PARAMS = (1 << 2),
        AV_CODEC_EXPORT_DATA_FILM_GRAIN = (1 << 3),
    }
    public unsafe struct AVCodecContext
    {
        // AVClass*
        public AVClass* av_class;
        public int log_level_offset;
        public AVMediaType codec_type;
        // 
        public AVCodec* codec;
        public AVCodecID codec_id;
        public uint codec_tag;

        public void* priv_data;
        public void* _internal;
        public void* opaque;
        public long bit_rate;
        public int bit_rate_tolerance;
        public int global_quality;
        public int compression_level;
        public AVCodecContextFlags flags;
        public AVCodecContextFlags2 flags2;
        public byte* extradata;
        public int extradata_size;
        public AVRational time_base;
        public int ticks_per_frame;
        public int delay;
        public int width;
        public int height;
        public int coded_width;
        public int coded_height;
        public int gopsize;
        public AVPixelFormat pix_fmt;

        // void (* draw_horiz_band) (struct AVCodecContext *s, const AVFrame *src, int offset[AV_NUM_DATA_POINTERS], int y, int type, int height);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, AVFrame*, int*, int, int, int, void> draw_horiz_band;
        // enum AVPixelFormat (*get_format)(struct AVCodecContext *s, const enum AVPixelFormat * fmt);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, AVPixelFormat*, AVPixelFormat> get_format;

        public int max_b_frames;
        public float b_quant_factor;
        public float b_quant_offset;
        public int has_b_frames;
        public float i_quant_factor;
        public float i_quant_offset;
        public float lumi_masking;
        public float temporal_cplx_masking;
        public float spatial_cplx_masking;
        public float p_masking;
        public float dark_masking;
        public int slice_count;
        public int* slice_offset;
        public AVRational sample_aspect_ratio;
        public int me_cmp;
        public int me_sub_cmp;
        public int mb_cmp;
        public int ildct_cmp;
        public int dia_size;
        public int last_predictor_count;
        public int me_pre_cmp;
        public int pre_dia_size;
        public int me_subpel_quality;
        public int me_range;
        public int slice_flags;
        public int mbdecision;
        public ushort* intra_matrix;
        public ushort* inter_matrix;
        public int intra_dc_precision;
        public int skip_top;
        public int skip_bottom;
        public int mb_lmin;
        public int mb_lmax;
        public int bidir_refine;
        public int keyint_min;
        public int refs;
        public int mv0_threshold;
        public AVColorPrimaries color_primaries;
        public AVColorTransferCharacteristic color_trc;
        public AVColorSpace colorspace;
        public AVColorRange color_range;
        public AVChromaLocation chroma_sample_location;
        public int slices;
        public AVFieldOrder field_order;
        public int sample_rate;
        public int channels;
        public AVSampleFormat sample_fmt;
        public int frame_size;
        public int frame_number;
        public int block_align;
        public int cutoff;
        public ulong channel_layout;
        public ulong request_channel_layout;
        public AVAudioServiceType audio_service_type;
        public AVSampleFormat request_sample_fmt;

        // int (* get_buffer2) (struct AVCodecContext *s, AVFrame *frame, int flags);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, AVFrame*, int, int> get_buffer2;
        public float qcompress;
        public float qblur;
        public int qmin;
        public int qmax;
        public int max_qdiff;
        public int rc_buffer_size;
        public int rc_override_count;
        public RcOverride* rc_override;
        public long rc_max_rate;
        public long rc_min_rate;
        public float rc_max_available_vbv_use;
        public float rc_min_vbv_overflow_use;
        public int rc_initial_buffer_occupancy;
        public int trellis;
        public char* stats_out;
        public char* stats_in;
        public int workaround_bugs;
        public int strict_std_compliance;
        public int error_concealment;
        public int debug;
        public int err_recognition;
        public long reordered_opaque;
        public AVHWAccel* hwaccel;
        public void* hwaccel_context;
        public ulong* error;
        public int dct_algo;
        public int idct_algo;
        public int bits_per_coded_sample;
        public int bits_per_raw_sample;
        public int lowres;
        public int thread_count;
        public int thread_type;
        public int active_thread_type;
        public int thread_safe_callbacks;
        // int (*execute)(struct AVCodecContext *c, int (*func)(struct AVCodecContext *c2, void *arg), void *arg2, int *ret, int count, int size);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, delegate* unmanaged[Cdecl]<AVCodecContext*, void*, int>, void*, int*, int, int, int> execute;

        // int (*execute2)(struct AVCodecContext *c, int (*func)(struct AVCodecContext *c2, void *arg, int jobnr, int threadnr), void *arg2, int *ret, int count);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, delegate* unmanaged[Cdecl]<AVCodecContext*, void*, int, int, int>, void*, int*, int, int> execute2;

        public int nsse_weight;
        public int profile;
        public int level;
        public AVDiscard skip_loop_filter;
        public AVDiscard skip_idct;
        public AVDiscard skip_frame;
        public byte* subtitle_header;
        public int subtitle_header_size;
        public int initial_padding;
        public AVRational framerate;
        public AVPixelFormat sw_pix_fmt;
        public AVRational pkt_timebase;
        public AVCodecDescriptor* codec_descriptor;
        public long pts_correction_num_faulty_pts;
        public long pts_correction_num_faulty_dts;
        public long pts_correction_last_pts;
        public long pts_correction_last_dts;

        public byte* sub_charenc;
        public int sub_charenc_mode;

        public int skip_alpha;
        public int seek_preroll;
        public int debug_mv;
        public ushort* chroma_intra_matrix;
        public byte* dump_separator;
        public byte* codec_whitelist;
        public uint properties;
        public AVPacketSideData* coded_side_data;
        public int nb_coded_side_data;
        public AVBufferRef* hw_frames_ctx;
        public int sub_text_format;
        public int trailing_padding;
        public long max_pixels;
        public AVBufferRef* hw_device_ctx;
        public int hwaccel_flags;
        public int apply_cropping;
        public int extra_hw_frames;
        public int discard_damaged_percentage;
        public long max_samples;
        public AVCodecContextExportSideDataFlags export_side_data;
        // int (*get_encode_buffer)(struct AVCodecContext *s, AVPacket *pkt, int flags);
        public delegate* unmanaged[Cdecl]<AVCodecContext*, AVPacket*, int, int> get_encode_buffer;
    }
    public struct RcOverride
    {
        public int start_frame;
        public int end_frame;
        public int qscale;
        public float quality_factor;
    }
}