// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    public static unsafe class AVOptionsC
    {
        public static AVOption* SetIntList(void* obj, string name, int[] vals, int flags)
        {
            fixed (int* ptr = &vals[0])
            {
                return av_opt_set_bin(obj, name, (byte*)ptr, vals.Length * sizeof(int), flags);
            }
        }

        [DllImport(LibNames.LibAvUtil, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe AVOption* av_opt_find2(void* obj, string name, string unit, int opt_flags, int search_flags, void** target_obj);

        [DllImport(LibNames.LibAvUtil, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe AVOption* av_opt_set_bin(void* obj, string name, byte* val, int len, int search_flags);
    }
}
