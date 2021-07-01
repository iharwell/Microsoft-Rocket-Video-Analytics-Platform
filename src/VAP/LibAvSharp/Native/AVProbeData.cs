// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVProbeData
    {
        public byte* filename;
        public byte* buf;
        public int buf_size;
        public byte* mime_type;
    }
}