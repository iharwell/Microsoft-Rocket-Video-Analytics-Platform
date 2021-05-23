using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    /// <summary>
    /// Interface for a single frame of a video feed.
    /// </summary>
    public interface IFrame
    {
        /// <summary>
        /// The pixel data of the raw frame.
        /// </summary>
        byte[] FrameData { get; set; }

        /// <summary>
        /// The name of the video feed that this frame is from.
        /// </summary>
        string SourceName { get; set; }

        /// <summary>
        /// The index of this frame within the source feed.
        /// </summary>
        int FrameIndex { get; set; }
    }
}
