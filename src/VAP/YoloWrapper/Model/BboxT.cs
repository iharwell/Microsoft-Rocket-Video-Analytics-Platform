// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Wrapper.Yolo.Model
{
    internal struct BboxT
    {
        internal UInt32 _x, _y, _w, _h;    // (x,y) - top-left corner, (w, h) - width & height of bounded box
        internal float _prob;                 // confidence - probability that the object was found correctly
        internal UInt32 _obj_id;        // class of object - from range [0, classes-1]
        internal UInt32 _track_id;      // tracking id for video (0 - untracked, 1 - inf - tracked object)
        internal UInt32 _frames_counter;
    };
}
