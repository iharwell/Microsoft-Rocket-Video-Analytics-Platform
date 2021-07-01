// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVCodecParameters
    {
        public AVMediaType codec_type;
        public AVCodecID codec_id;
        public uint codec_tag;
        public byte* extradata;
        public int extradata_size;
        public int format;
        public long bit_rate;
        public int bits_per_coded_sample;
        public int bits_per_raw_sample;
        public int profile;
        public int level;
        public int width;
        public int height;
        public AVRational sample_aspect_ratio;
        public AVFieldOrder field_order;
        public AVColorRange color_range;
        public AVColorPrimaries color_primaries;
        public AVColorTransferCharacteristic color_trc;
        public AVColorSpace color_space;
        public AVChromaLocation chroma_location;
        public int video_delay;
        public ulong channel_layout;
        public int channels;
        public int sample_rate;
        public int block_align;
        public int frame_size;
        public int initial_padding;
    }
}