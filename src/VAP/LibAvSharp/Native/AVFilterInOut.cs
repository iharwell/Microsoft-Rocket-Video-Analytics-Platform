// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVFilterInOut
    {
        public byte* name;
        public AVFilterContext* filter_ctx;
        public int pad_idx;
        public AVFilterInOut* next;
    }
}
