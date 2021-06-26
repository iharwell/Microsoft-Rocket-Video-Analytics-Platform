using System;

namespace LibAvSharp.Native
{
    unsafe public struct AVFormatContext
    {
        // AVClass*
        public AVClass* av_class;
        // AVInputFormat*
        public AVInputFormat* iformat;
        // AVOutputFormat*
        public AVOutputFormat* oformat;
        public void* priv_data;
        // AVIOContext*
        public AVIOContext* pb;
        public int ctx_flags;
        public uint nb_streams;
        // AVStream**
        public AVStream** streams;
        public byte* url;
        public long start_time;
        public long duration;
        public long bit_rate;
        public uint packet_size;
        public int max_delay;
        public int flags;
        public long probesize;
        public long max_analyze_duration;
        public byte* key;
        public int keylen;
        public uint nb_programs;
        public AVProgram ** programs;
        public AVCodecID video_codec_id;
        public AVCodecID audio_codec_id;
        public AVCodecID subtitle_codec_id;
        public uint max_index_size;
        public uint max_picture_buffer;
        public uint nb_chapters;
        public AVChapter ** chapters;
        public AVDictionary * metadata;
        public long start_time_realtime;
        public int fps_probe_size;
        public int error_recognition;
        public AVIOInteruptCB interupt_callback;
        public int debug;
        public long max_interleave_delta;
        public int strict_std_compliance;
        public int event_flags;
        public int max_ts_probe;
        public int avoid_negative_ts;
        public int ts_id;
        public int audio_preload;
        public int max_chunk_duration;
        public int max_chunk_size;
        public int use_wallclock_as_timestamps;
        public int avio_flags;
        public AVDurationEstimationMethod duration_estimation_method;
        public long skip_initial_bytes;
        public uint correct_ts_overflow;
        public int seek2any;
        public int flush_packets;
        public int probe_score;
        public int format_probesize;
        public byte* codec_whitelist;
        public byte* format_whitelist;
        // public AVFormatInternal* _internal;
        public IntPtr _internal;
        public int io_repositioned;
        public AVCodec* video_codec;
        public AVCodec* audio_codec;
        public AVCodec* subtitle_codec;
        public AVCodec* data_codec;
        public int metadata_header_padding;
        public void* opaque;
        // typedef int (* av_format_control_message) (struct AVFormatContext *s, int type, void *data, size_t data_size);
        public delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, long> control_message_cb;
        public long output_ts_offset;
        public byte* dump_separator;
        public AVCodecID data_codec_id;
        public byte* protocol_whitelist;

        // int (* io_open) (struct AVFormatContext *s, AVIOContext **pb, const char *url, int flags, AVDictionary **options);
        public delegate* unmanaged[Cdecl]<IntPtr,IntPtr, string, int, IntPtr, int> io_open;

        // void (* io_close) (struct AVFormatContext *s, AVIOContext *pb);
        public delegate* unmanaged[Cdecl]<IntPtr,IntPtr, void> io_close;

        public byte* protocol_blacklist;
        public int max_streams;
        public int skip_estimate_duration_from_pts;
        public int max_probe_packets;
    }
}