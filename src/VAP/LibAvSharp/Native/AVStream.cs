// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    public unsafe struct AVStream
    {
        public int index;
        public int id;
        public void* priv_data;
        public AVRational time_base;
        public long start_time;
        public long duration;
        public long nb_frames;
        public int disposition;
        public AVDiscard discard;
        public AVRational sample_aspect_ratio;
        public AVDictionary* metadata;
        public AVRational avg_frame_rate;
        public AVPacket attached_pic;
        public AVPacketSideData* side_data;
        public int nb_side_data;
        public int event_flags;
        public AVRational r_frame_rate;
        public AVCodecParameters* codecpar;
        public int pts_wrap_bits;
        public long first_dts;
        public long cur_dts;
        // AVStreamInternal* internal;
        public IntPtr intern;
    }
}
