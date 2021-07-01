// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVIOInteruptCB
    {
        public delegate* unmanaged[Cdecl]<void*, int> callback;
        public void* opaque;
    }
}