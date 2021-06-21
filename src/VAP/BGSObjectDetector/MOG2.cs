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

        public UMat DetectForeground(UMat image, int nFrames)
        {
            UMat fgMask0 = new UMat(UMatUsageFlags.DeviceMemory);
            fgDetector.Apply(image, fgMask0);

            if (regionOfInterest != null)
            {
                UMat fgMask = new UMat(UMatUsageFlags.DeviceMemory);
                Cv2.BitwiseAnd(fgMask0, regionOfInterest, fgMask);
                fgMask0.Dispose();
                fgMask0 = fgMask;
            }

            if (nFrames < N_FRAMES_TO_LEARN)
            {
                fgMask0.Dispose();
                return null;
            }
            else
            {
                return fgMask0;
            }
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
