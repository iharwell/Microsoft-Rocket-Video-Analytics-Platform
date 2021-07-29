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
    public class Frame : IFrame, ISerializable, IDisposable
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
            FrameSize = new System.Drawing.Size(frameData.Width, frameData.Height);
        }

        protected Frame(SerializationInfo info, StreamingContext context)
        {
            SourceName = info.GetString(nameof(SourceName));
            FrameIndex = info.GetInt32(nameof(FrameIndex));
            FrameRate = info.GetSingle(nameof(FrameRate));
            FileFrameIndex = info.GetInt32(nameof(FileFrameIndex));
            CameraName = info.GetString(nameof(CameraName));
            TimeStamp = (DateTime)info.GetValue(nameof(TimeStamp), typeof(DateTime));

            int width = info.GetInt32(nameof(FrameSize.Width));
            int height = info.GetInt32(nameof(FrameSize.Height));

            FrameSize = new System.Drawing.Size(width, height);

            LastKeyFrame = info.GetInt32(nameof(LastKeyFrame));
            //object framebytes = info.GetValue(nameof(FrameData), typeof(byte[]));

            //FrameData = Mat.FromImageData(framebytes as byte[], ImreadModes.Color);


            //object o = info.GetValue(nameof(ForegroundMask), typeof(byte[]));

            /*if (o is null)
            {
                ForegroundMask = null;
            }
            else
            {
                ForegroundMask = Mat.FromImageData(o as byte[], ImreadModes.AnyColor);
            }*/
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
            FrameSize = new System.Drawing.Size(frameData.Width, frameData.Height);
            TimeStamp = timeStamp;
        }



        /// <inheritdoc />
        public Mat FrameData
        {
            get
            {
                return _frameData;
            }
            set
            {
                _frameData = value;
                FrameSize = new System.Drawing.Size(value.Width, value.Height);
            }
        }

        /// <inheritdoc />
        public Mat ForegroundMask { get; set; }

        public System.Drawing.Size FrameSize { get; set; }

        /// <inheritdoc />
        public string SourceName { get; set; }

        /// <inheritdoc />
        public int FrameIndex { get; set; }

        /// <inheritdoc />
        public int FileFrameIndex { get; set; }

        /// <inheritdoc />
        public DateTime TimeStamp { get; set; }

        /// <inheritdoc />
        public string CameraName { get; set; }

        /// <inheritdoc />
        public float FrameRate { get; set; }


        public long LastKeyFrame { get; set; }

        /// <inheritdoc />
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(SourceName), SourceName);
            info.AddValue(nameof(FileFrameIndex), FileFrameIndex);
            info.AddValue(nameof(TimeStamp), TimeStamp);
            info.AddValue(nameof(CameraName), CameraName);
            info.AddValue(nameof(FrameIndex), FrameIndex);
            info.AddValue(nameof(FrameRate), FrameRate);

            info.AddValue(nameof(FrameSize.Width), FrameSize.Width);
            info.AddValue(nameof(FrameSize.Height), FrameSize.Height);

            info.AddValue(nameof(LastKeyFrame), LastKeyFrame);
            /*info.AddValue(nameof(FrameData), FrameData.ToBytes(".jpg", s_encodingParamsJPG));
            if (ForegroundMask != null)
            {
                info.AddValue(nameof(ForegroundMask), ForegroundMask.ToBytes(".png", s_encodingParamsPNG));
            }
            else
            {
                info.AddValue(nameof(ForegroundMask), null);
            }
            */
        }

        private static readonly ImageEncodingParam[] s_encodingParamsJPG = new ImageEncodingParam[]
        {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 50),
            new ImageEncodingParam(ImwriteFlags.JpegOptimize, 1)
        };
        private static readonly ImageEncodingParam[] s_encodingParamsPNG = new ImageEncodingParam[]
        {
            new ImageEncodingParam(ImwriteFlags.PngCompression, 7 ),
            new ImageEncodingParam(ImwriteFlags.PngBilevel, 1 )
        };
        private bool _disposedValue;
        private Mat _frameData;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (FrameData != null)
                    {
                        FrameData.Dispose();
                    }
                    FrameData = null;
                    if (ForegroundMask != null)
                    {
                        ForegroundMask.Dispose();
                    }
                    ForegroundMask = null;
                }

                /*if (FrameData != null)
                {
                    //FrameData.Dispose();
                    FrameData = null;
                }
                if (ForegroundMask != null)
                {
                    //ForegroundMask.Dispose();
                    ForegroundMask = null;
                }*/

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Frame()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
