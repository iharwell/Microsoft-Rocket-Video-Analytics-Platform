using System;
using System.Runtime.InteropServices;

namespace DarknetAAB
{
    [StructLayout(LayoutKind.Sequential)]
    public struct bbox_t
    {
        public UInt32 x, y, w, h;    // (x,y) - top-left corner, (w, h) - width & height of bounded box
        public float prob;           // confidence - probability that the object was found correctly
        public UInt32 obj_id;        // class of object - from range [0, classes-1]
        public UInt32 track_id;      // tracking id for video (0 - untracked, 1 - inf - tracked object)
        public UInt32 frames_counter;
        public float x_3d, y_3d, z_3d;  // 3-D coordinates, if there is used 3D-stereo camera
    }
}
