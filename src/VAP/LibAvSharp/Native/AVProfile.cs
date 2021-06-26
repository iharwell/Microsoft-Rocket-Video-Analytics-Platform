using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LibAvSharp.Native
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct AVProfile
    {
        int profile;
        byte* name;
    }
}
