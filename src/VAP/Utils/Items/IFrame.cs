﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using OpenCvSharp;

namespace Utils.Items
{
    /// <summary>
    ///   Interface for a single frame of a video feed.
    /// </summary>
    public interface IFrame
    {
        /// <summary>
        ///   The foreground mask for this frame if one is available.
        /// </summary>
        Mat ForegroundMask { get; set; }

        /// <summary>
        ///   The pixel data of the raw frame.
        /// </summary>
        Mat FrameData { get; set; }

        /// <summary>
        ///   The name of the video feed that this frame is from.
        /// </summary>
        string SourceName { get; set; }

        /// <summary>
        ///   The index of this frame within the source feed.
        /// </summary>
        int FrameIndex { get; set; }

        /// <summary>
        ///   The name of the camera that recorded this frame;
        /// </summary>
        string CameraName { get; set; }

        /// <summary>
        ///   The index of this frame within the specific source file it came from.
        /// </summary>
        int FileFrameIndex { get; set; }

        /// <summary>
        ///   The time that the frame was taken.
        /// </summary>
        DateTime TimeStamp { get; set; }

        /// <summary>
        ///   The time that the frame was taken.
        /// </summary>
        float FrameRate { get; set; }
    }
}
