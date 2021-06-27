// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVFilterCommand
    {
        public double time;
        public byte* command;
        public byte* arg;
        public int flags;
        public AVFilterCommand* next;
    }
}
