namespace LibAvSharp.Native
{
    unsafe public struct AVDeviceInfoList
    {
        public AVDeviceInfo** devices;
        public int nb_devices;
        public int default_device;
    }
}