namespace LibAvSharp.Native
{
    unsafe public struct AVHWAccel
    {
        public char* name;
        public AVMediaType type;
        public AVCodecID id;
        public AVPixelFormat pix_fmt;
        public int capabilities;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, AVFrame*, int> alloc_frame;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, int, byte*, uint,int> decode_params;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, byte*, uint, int> decode_slice;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, int> end_frame;
        private int frame_priv_data_size;
        private delegate* unmanaged[Cdecl]<void*, int> decode_mb;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, int> init;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, int> uninit;
        private int priv_data_size;
        private int caps_internal;
        private delegate* unmanaged[Cdecl]<AVCodecContext*, AVBufferRef*, int> frame_params;
    }
}