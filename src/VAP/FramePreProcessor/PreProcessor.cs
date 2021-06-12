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

            if (frameIndex % samplingFactor != 0) return null;

            try
            {
                //resizedFrame = sourceMat.Clone();
                if (display)
                    FrameDisplay.Display(sourceMat);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("********RESET RESIZE*****");
                return null;
            }
            return sourceMat;
        }
    }
}
