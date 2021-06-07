// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

using OpenCvSharp;

namespace FramePreProcessor
{
    public class PreProcessor
    {
        public static Mat ReturnFrame(Mat sourceMat, int frameIndex, int samplingFactor, double resolutionFactor, bool display)
        {
            Mat resizedFrame = null;

            if (frameIndex % samplingFactor != 0) return resizedFrame;

            try
            {
                resizedFrame = sourceMat.Resize(new OpenCvSharp.Size((int)(sourceMat.Size().Width * resolutionFactor), (int)(sourceMat.Size().Height * resolutionFactor)));
                if (display)
                    FrameDisplay.Display(resizedFrame);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("********RESET RESIZE*****");
                return null;
            }
            return resizedFrame;
        }
    }
}
