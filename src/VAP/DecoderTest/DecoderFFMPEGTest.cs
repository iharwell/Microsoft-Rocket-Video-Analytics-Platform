using System;
using Xunit;
using Decoder;

namespace DecoderTest
{
    public class DecoderFFMPEGTest
    {
        [Fact]
        public void ConstructionTest()
        {
            DecoderFFMPEG decoder = DecoderFFMPEG.GetDirectoryDecoder("F:\\SamFrontYard\\FrontYardTest2", 0.25);
        }
        [Fact]
        public void DisposeTest()
        {
            DecoderFFMPEG decoder = DecoderFFMPEG.GetDirectoryDecoder("F:\\SamFrontYard\\FrontYardTest2", 0.25);
            decoder.Dispose();
        }
        [Fact]
        public void RunDisposeTest()
        {
            DecoderFFMPEG decoder = DecoderFFMPEG.GetDirectoryDecoder("F:\\SamFrontYard\\FrontYardTest2", 0.25);
            decoder.BeginReading();
            while ( decoder.HasMoreFrames)
            {
                var frame = decoder.GetNextFrame();
                if(frame is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            decoder.Dispose();
        }
    }
}
