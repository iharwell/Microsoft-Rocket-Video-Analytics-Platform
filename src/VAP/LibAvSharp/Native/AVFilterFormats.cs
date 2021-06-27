// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVFilterFormats
    {
        public uint nb_formats;
        public int* formats;
        public uint refcount;
        public AVFilterFormats*** refs;
    }
}
