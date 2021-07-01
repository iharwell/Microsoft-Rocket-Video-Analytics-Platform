// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibAvSharp
{
    public static class RationalC
    {
        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AVRational av_make_q(int num, int den);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int av_cmp_q(AVRational a, AVRational b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int av_reduce(AVRational a);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AVRational av_mul_q(AVRational a, AVRational b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AVRational av_div_q(AVRational a, AVRational b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AVRational av_add_q(AVRational a, AVRational b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AVRational av_sub_q(AVRational a, AVRational b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AVRational av_inv_q(AVRational a);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AVRational av_d2q(double d, int max);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int av_nearer_q(AVRational q, AVRational q1, AVRational q2);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int av_find_nearest_q_idx(AVRational q, AVRational[] q_list);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint av_q2intfloat(AVRational a);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AVRational av_gdc_q(AVRational a, AVRational b, int max_den, AVRational def);
    }
}
