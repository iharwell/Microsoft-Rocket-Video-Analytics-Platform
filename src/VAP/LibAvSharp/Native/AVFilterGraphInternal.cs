// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVFilterGraphInternal
    {
        public void* thread;
        public delegate* unmanaged[Cdecl]<AVFilterContext*, delegate* unmanaged[Cdecl]<AVFilterContext*, void*, int, int, int>, void*, int*, int, int> execute;
        public FFFrameQueueGlobal frame_queues;
    }
}
