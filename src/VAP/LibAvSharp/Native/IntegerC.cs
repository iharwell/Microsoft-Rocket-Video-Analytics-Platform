// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace LibAvSharp
{
    internal static class IntegerC
    {
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_add_i")]
        internal static extern AVInteger av_add_i(AVInteger a, AVInteger b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_sub_i")]
        internal static extern AVInteger av_sub_i(AVInteger a, AVInteger b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_log2_i")]
        internal static extern int av_log2_i(AVInteger a);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_mul_i")]
        internal static extern AVInteger av_mul_i(AVInteger a, AVInteger b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_cmp_i")]
        internal static extern int av_cmp_i(AVInteger a, AVInteger b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_shr_i")]
        internal static extern AVInteger av_shr_i(AVInteger a, int s);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_mod_i")]
        internal static extern AVInteger av_mod_i(ref AVInteger quot, AVInteger a, AVInteger b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_div_i")]
        internal static extern AVInteger av_div_i(AVInteger a, AVInteger b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AVInteger av_int2i(long a);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern long av_i2int(AVInteger a);
    }
}
