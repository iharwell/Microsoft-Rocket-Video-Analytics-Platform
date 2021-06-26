namespace LibAvSharp.Native
{
    unsafe public struct AVProbeData
    {
        public byte* filename;
        public byte* buf;
        public int buf_size;
        public byte* mime_type;
    }
}