// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;

namespace Utils.Items
{
    /// <summary>
    ///   Default implementation of the <see cref="IFrame" /> interface.
    /// </summary>
    public class Frame : IFrame
    {
        public Frame()
        {

        }

        /// <summary>
        ///   Creates a <see cref="Frame" /> object.
        /// </summary>
        /// <param name="sourceName">
        ///   The name of the source of the frame, such as a URL, a camera identifier, etc.
        /// </param>
        /// <param name="frameIndex">
        ///   The index of the frame within the source.
        /// </param>
        /// <remarks>
        ///   This constructor does not set up any frame data. This must be added manually, and is
        ///   highly recommended to avoid breaking some algorithms that depend on it being present.
        /// </remarks>
        public Frame(string sourceName, int frameIndex)
        {
            SourceName = sourceName;
            FrameIndex = frameIndex;
        }

        /// <inheritdoc cref="Frame(string, int, byte[], DateTime)" />
        public Frame(string sourceName, int frameIndex, Mat frameData)
        {
            SourceName = sourceName;
            FrameIndex = frameIndex;
            FrameData = frameData;
        }

        /// <summary>
        ///   Creates a <see cref="Frame" /> object.
        /// </summary>
        /// <param name="sourceName">
        ///   The name of the source of the frame, such as a URL, a camera identifier, etc.
        /// </param>
        /// <param name="frameIndex">
        ///   The index of the frame within the source.
        /// </param>
        /// <param name="frameData">
        ///   The actual image data for the frame.
        /// </param>
        /// <param name="timeStamp">
        ///   The timestamp of the frame.
        /// </param>
        public Frame(string sourceName, int frameIndex, Mat frameData, DateTime timeStamp)
        {
            SourceName = sourceName;
            FrameIndex = frameIndex;
            FrameData = frameData;
            TimeStamp = TimeStamp;
        }

        /// <inheritdoc />
        public Mat FrameData { get; set; }

        /// <inheritdoc />
        public Mat ForegroundMask { get; set; }

        /// <inheritdoc />
        public string SourceName { get; set; }

        /// <inheritdoc />
        public int FrameIndex { get; set; }

        /// <inheritdoc />
        public DateTime TimeStamp { get; set; }
    }
}
