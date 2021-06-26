using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    unsafe public static class SWScaleC
    {
        [DllImport("swscale-6.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr sws_getCachedContext( IntPtr context,
                                                          int srcW, int srcH, AVPixelFormat srcFormat,
                                                          int dstW, int dstH, AVPixelFormat dstFormat,
                                                          SWS_Flags flags, IntPtr srcFilter, IntPtr dstFilter,
                                                          [In] double* param );

        [DllImport("swscale-6.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int sws_scale( IntPtr context, [In] byte** srcSlice,
                                            [In] int* srcH, int srcSliceY, int srcSliceH,
                                            [Out,In] byte** dst, [In] int* dstStride );
    }
}
