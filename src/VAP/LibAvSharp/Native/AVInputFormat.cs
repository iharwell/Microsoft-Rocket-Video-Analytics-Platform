namespace LibAvSharp.Native
{
    unsafe public struct AVInputFormat
    {
        public byte* name;
        public byte* long_name;
        public int flags;
        public byte* extensions;
        public AVCodecTag** codec_tag;
        public AVClass* priv_class;
        public byte* mime_type;
        public int raw_codec_id;
        public int priv_data_size;
        public delegate* unmanaged[Cdecl]<AVProbeData*, int> read_probe;
        public delegate* unmanaged[Cdecl]<AVFormatContext*, int> read_header;
        public delegate* unmanaged[Cdecl]<AVFormatContext*,AVPacket*, int> read_packet;
        public delegate* unmanaged[Cdecl]< AVFormatContext*,int> read_close;
        public delegate* unmanaged[Cdecl]< AVFormatContext*,int,long,int,int> read_seek;
        public delegate* unmanaged[Cdecl]< AVFormatContext*,int,long*,long,long> read_timestamp;
        public delegate* unmanaged[Cdecl]< AVFormatContext*,int> read_play;
        public delegate* unmanaged[Cdecl]< AVFormatContext*,int> read_pause;
        public delegate* unmanaged[Cdecl]< AVFormatContext*,int,long,long,long,int,int> read_seek2;
        public delegate* unmanaged[Cdecl]< AVFormatContext*,AVDeviceInfoList*,int> get_device_list;
    }
}