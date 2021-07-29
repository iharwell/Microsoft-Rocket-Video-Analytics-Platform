// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace LibAvSharp.Native
{
    public enum AVIODirEntryType : int
    {
        AVIO_ENTRY_UNKNOWN,
        AVIO_ENTRY_BLOCK_DEVICE,
        AVIO_ENTRY_CHARACTER_DEVICE,
        AVIO_ENTRY_DIRECTORY,
        AVIO_ENTRY_NAMED_PIPE,
        AVIO_ENTRY_SYMBOLIC_LINK,
        AVIO_ENTRY_SOCKET,
        AVIO_ENTRY_FILE,
        AVIO_ENTRY_SERVER,
        AVIO_ENTRY_SHARE,
        AVIO_ENTRY_WORKGROUP,
    };
    public static unsafe class AVIOC
    {
        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl)]
        public static extern string avio_find_protocol_name([In] string url);


        [DllImport(LibNames.LibAvFormat, CallingConvention = CallingConvention.Cdecl)]
        public static extern int avio_check([In] string url, int flags);


        [DllImport(LibNames.LibAvUtil, CallingConvention = CallingConvention.Cdecl)]
        public static extern int avio_alloc_context([In] byte* buffer, int buffer_size, int write_flags, void* opaque,
                                                     delegate* unmanaged[Cdecl]<void*, byte*, int, int> read_packet,
                                                     delegate* unmanaged[Cdecl]<void*, byte*, int, int> write_packet,
                                                     delegate* unmanaged[Cdecl]<void*, long, int, long> offset);

        // int av_file_map(const char *filename, uint8_t **bufptr, size_t *size, int log_offset, void *log_ctx);
    }
}
