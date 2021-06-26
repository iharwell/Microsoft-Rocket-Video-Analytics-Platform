using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    unsafe public struct AVPacket
    {
        public AVBufferRef* buf;
        public long pts;
        public long dts;
        public byte* data;
        public int size;
        public int stream_index;
        public int flags;
        public AVPacketSideData* side_data;
        public int side_data_elems;
        public long duration;
        public long pos;
    }
}
