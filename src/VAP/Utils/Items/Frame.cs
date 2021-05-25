using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Items
{
    public class Frame : IFrame
    {
        public Frame()
        {

        }

        public Frame( string sourceName, int frameIndex )
        {
            SourceName = sourceName;
            FrameIndex = frameIndex;
        }
        public Frame( string sourceName, int frameIndex, byte[] frameData )
        {
            SourceName = sourceName;
            FrameIndex = frameIndex;
            FrameData = frameData;
        }

        public byte[] FrameData { get; set; }
        public string SourceName { get; set; }
        public int FrameIndex { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
