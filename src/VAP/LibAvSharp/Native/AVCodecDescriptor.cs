

using System.Runtime.InteropServices;

namespace LibAvSharp.Native
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct AVCodecDescriptor
    {
        public AVCodecID id;
        public AVMediaType type;
        public byte* name;
        public byte* long_name;
        public int props;
        public byte* mime_types;
        public AVProfile* profiles;
    }
}