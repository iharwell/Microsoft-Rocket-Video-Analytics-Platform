// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using OpenCvSharp;

namespace Utils.Items
{
    /// <summary>
    ///   Default implementation of the <see cref="IFrame" /> interface. Represents a single frame
    ///   in a video.
    /// </summary>
    [Serializable]
    public class Frame : IFrame, ISerializable
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

        protected Frame(SerializationInfo info, StreamingContext context)
        {
            SourceName = info.GetString(nameof(SourceName));
            FrameIndex = info.GetInt32(nameof(FrameIndex));
            TimeStamp = (DateTime)info.GetValue(nameof(TimeStamp), typeof(DateTime));

            object framebytes = info.GetValue(nameof(FrameData), typeof(byte[]));

            FrameData = Mat.FromImageData(framebytes as byte[], ImreadModes.Color);


            object o = info.GetValue(nameof(ForegroundMask), typeof(byte[]));

            if (o is null)
            {
                ForegroundMask = null;
            }
            else
            {
                ForegroundMask = Mat.FromImageData(o as byte[], ImreadModes.AnyColor);
            }
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
            TimeStamp = timeStamp;
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

        /// <inheritdoc />
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(SourceName), SourceName);
            info.AddValue(nameof(FrameIndex), FrameIndex);
            info.AddValue(nameof(TimeStamp), TimeStamp);
            info.AddValue(nameof(FrameData), FrameData.ToBytes(".jpg", s_encodingParamsJPG));
            if (ForegroundMask != null)
            {
                info.AddValue(nameof(ForegroundMask), ForegroundMask.ToBytes(".png", s_encodingParamsPNG));
            }
            else
            {
                info.AddValue(nameof(ForegroundMask), null);
            }

        }

        private static readonly ImageEncodingParam[] s_encodingParamsJPG = new ImageEncodingParam[]
        {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 70),
            new ImageEncodingParam(ImwriteFlags.JpegOptimize, 1)
        };
        private static readonly ImageEncodingParam[] s_encodingParamsPNG = new ImageEncodingParam[]
        {
            new ImageEncodingParam(ImwriteFlags.PngCompression, 7 ),
            new ImageEncodingParam(ImwriteFlags.PngBilevel, 1 )
        };
    }
}
