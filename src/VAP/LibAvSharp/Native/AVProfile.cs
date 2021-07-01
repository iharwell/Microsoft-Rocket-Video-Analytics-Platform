// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LibAvSharp.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AVProfile
    {
        private int profile;
        private byte* name;
    }
}
