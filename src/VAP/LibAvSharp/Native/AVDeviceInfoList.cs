// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVDeviceInfoList
    {
        public AVDeviceInfo** devices;
        public int nb_devices;
        public int default_device;
    }
}