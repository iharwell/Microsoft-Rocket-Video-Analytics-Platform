// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVChapter
    {
        public long id;
        public AVRational time_base;
        public long start;
        public long end;
        public AVDictionary* metadata;
    }
}