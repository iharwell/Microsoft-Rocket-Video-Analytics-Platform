using System.Runtime.InteropServices;

namespace DarknetAAB
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BboxContainer
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = YoloWrapper.MaxObjects)]
        public bbox_t[] candidates;
    }
}
