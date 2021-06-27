// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public enum AVFilterLinkState : int
    {
        AVLINK_UNINIT = 0,
        AVLINK_STARTINIT,
        AVLINK_INIT
    }

    public unsafe struct AVFilterLink
    {
        public AVFilterContext* src;
        public AVFilterPad* srcpad;
        public AVFilterContext* dst;
        public AVFilterPad* dstpad;
        public AVMediaType type;
        public int w;
        public int h;
        public AVRational sample_aspect_ratio;
        public ulong channel_layout;
        public int sample_rate;
        public int format;
        public AVRational time_base;
        public AVFilterFormats* in_formats;
        public AVFilterFormats* out_formats;
        public AVFilterFormats* in_sample_layouts;
        public AVFilterFormats* out_sample_layouts;
        public int request_samples;
        public AVFilterLinkState init_state;
        public AVFilterGraph* graph;
        public long current_pts;
        public long curent_pts_us;
        public int age_index;
        public AVRational frame_rate;
        public AVFrame* partial_buf;
        public int partial_buf_size;
        public int min_samples;
        public int max_samples;
        public int channels;
        public uint flags;
        public long frame_count_in;
        public long frame_count_out;
        public void* frame_pool;
        public int frame_wanted_out;
        public AVBufferRef* hw_frames_ctx;
        private fixed byte reserved[0xF000];
    }
}
