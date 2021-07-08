// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarknetAAB
{
    public class Yolo4Tiny : Yolo4DNN
    {
        public Yolo4Tiny()
            : base("yolov4-tiny.cfg", "yolov4-tiny.weights", 0)
        { }
        public Yolo4Tiny(int gpu)
            : base("yolov4-tiny.cfg", "yolov4-tiny.weights", gpu)
        { }
    }
    public class Yolo4Full : Yolo4DNN
    {
        public Yolo4Full()
            : base("yolov4.cfg", "yolov4.weights", 0)
        { }
        public Yolo4Full(int gpu)
            : base("yolov4.cfg", "yolov4.weights", gpu)
        { }
    }
}
