using System;
using System.Runtime.InteropServices;

namespace LibAvSharp
{
    internal static class IntegerC
    {
        [DllImport("avutil-57.dll", CallingConvention=CallingConvention.Cdecl, EntryPoint = "av_add_i")]
        static extern internal AVInteger av_add_i(AVInteger a, AVInteger b);


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_sub_i")]
        static extern internal AVInteger av_sub_i( AVInteger a, AVInteger b );


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_log2_i")]
        static extern internal int av_log2_i( AVInteger a );


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_mul_i")]
        static extern internal AVInteger av_mul_i( AVInteger a, AVInteger b );


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_cmp_i")]
        static extern internal int av_cmp_i( AVInteger a, AVInteger b );


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_shr_i")]
        static extern internal AVInteger av_shr_i( AVInteger a, int s );


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_mod_i")]
        static extern internal AVInteger av_mod_i( ref AVInteger quot, AVInteger a, AVInteger b );


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "av_div_i")]
        static extern internal AVInteger av_div_i( AVInteger a, AVInteger b );


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern internal AVInteger av_int2i( long a );


        [DllImport("avutil-57.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern internal long av_i2int( AVInteger a );
    }
}
