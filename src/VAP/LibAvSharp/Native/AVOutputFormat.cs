namespace LibAvSharp.Native
{
    unsafe public struct AVOutputFormat
    {
        public byte* name;
        public byte* long_name;
        public byte* mime_type;
        public byte* extensions;
        public AVCodecID audio_codec;
        public AVCodecID video_codec;
        public AVCodecID subtitle_codec;
        public int flags;
        public AVCodecTag** codec_tag;
        public AVClass* priv_class;
        public int priv_data_size;
        public delegate* unmanaged[Cdecl]<AVFormatContext*,int> write_header;
        public delegate* unmanaged[Cdecl]<AVFormatContext*,AVPacket*,int> write_packet;
        public delegate* unmanaged[Cdecl]<AVFormatContext*,int> write_trailer;
        public delegate* unmanaged[Cdecl]<AVFormatContext*, AVPacket*,AVPacket*,int,int> interleave_packet;
        public delegate* unmanaged[Cdecl]<AVCodecID, int, int> query_codec;
        public delegate* unmanaged[Cdecl]<AVFormatContext*,int,long*,long*,void> get_output_timestamp;
        public delegate* unmanaged[Cdecl]<AVFormatContext*,int,void*,long,int> control_message;
        public delegate* unmanaged[Cdecl]<AVFormatContext*,int,AVFrame**,uint,int> write_uncoded_frames;
        public delegate* unmanaged[Cdecl]<AVFormatContext*,AVDeviceInfoList*,int> get_device_list;
        public AVCodecID data_codec;
        public delegate* unmanaged[Cdecl]<AVFormatContext*,int> init;
        public delegate* unmanaged[Cdecl]< AVFormatContext*,void> deinit;
        public delegate* unmanaged[Cdecl]< AVFormatContext*,AVPacket*,int> check_bitstream;
    }
}