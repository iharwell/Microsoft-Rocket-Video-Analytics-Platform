// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

﻿using OpenCvSharp;

namespace BGSObjectDetector
{
    class MOG2
    {
        BackgroundSubtractorMOG2 fgDetector = BackgroundSubtractorMOG2.Create(500, 10); //try sweeping (also set it higher than 25)
        UMat regionOfInterest = null;

        int N_FRAMES_TO_LEARN = 120; // Why do we need this?

        public MOG2()
        {
            regionOfInterest = null;
        }

        public bool DetectForeground(UMat image, UMat output, int nFrames)
        {
            fgDetector.Apply(image, output);
            if (nFrames < N_FRAMES_TO_LEARN)
            {
                return false;
            }

            if (regionOfInterest != null)
            {
                UMat fgMask = new UMat(UMatUsageFlags.DeviceMemory);
                Cv2.BitwiseAnd(output, regionOfInterest, fgMask);
                fgMask.CopyTo(output);
                fgMask.Dispose();
            }

            return true;
        }

        public void SetRegionOfInterest(Mat roi)
        {
            if (roi != null)
            {
                regionOfInterest = new UMat();
                roi.CopyTo(regionOfInterest);
            }
        }
    }
}
