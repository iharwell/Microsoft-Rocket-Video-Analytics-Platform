// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVProgram
    {
        public int id;
        public int flags;
        public AVDiscard discard;
        public uint* stream_index;
        public uint nb_stream_indexes;
        public AVDictionary* metadata;
        public int program_num;
        public int pmt_pid;
        public int pcr_pid;
        public int pmt_version;
        public long start_time;
        public long end_time;
        public long pts_wrap_reference;
        public int pts_wrap_behavior;
    }
}