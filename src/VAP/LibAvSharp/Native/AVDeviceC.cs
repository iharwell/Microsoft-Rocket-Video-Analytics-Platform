﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibAvSharp.Native
{
    public static class AVDeviceC
    {
        [DllImport(LibNames.LibAvDevice, CallingConvention = CallingConvention.Cdecl)]
        public static extern void avdevice_register_all();
    }
}