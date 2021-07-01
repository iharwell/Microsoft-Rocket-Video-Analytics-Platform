// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVSubtitle
    {
        public ushort format;
        public uint start_display_time;
        public uint end_display_time;
        public uint num_rects;
        public AVSubtitleRect** rects;
        public long pts;
    }
}