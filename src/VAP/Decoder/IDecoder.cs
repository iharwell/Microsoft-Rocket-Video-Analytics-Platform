// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;

namespace Decoder
{
    public interface IDecoder
    {
        Mat GetNextFrame();

        int TotalFrameNumber { get; }

        double FramesPerSecond { get; }

        string FilePath { get; }
    }
}
