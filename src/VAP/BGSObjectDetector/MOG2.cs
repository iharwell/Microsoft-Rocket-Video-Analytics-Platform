// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using OpenCvSharp;

namespace BGSObjectDetector
{
    internal class MOG2
    {
        private const int N_FRAMES_TO_LEARN = 60;
        private readonly BackgroundSubtractorMOG2 _fgDetector = BackgroundSubtractorMOG2.Create(250, 20); //try sweeping (also set it higher than 25)

        private UMat _regionOfInterest = null;
        // Why do we need this?

        public UMat RegionOfInterest
        {
            get => _regionOfInterest;
            set => _regionOfInterest = value;
        }

        public MOG2()
        {
            _regionOfInterest = null;
        }

        public double BackgroundRatio
        {
            get => _fgDetector.BackgroundRatio;
            set => _fgDetector.BackgroundRatio = value;
        }

        public double ComplexityReductionThreshold
        {
            get => _fgDetector.ComplexityReductionThreshold;
            set => _fgDetector.ComplexityReductionThreshold = value;
        }

        public bool DetectShadows
        {
            get => _fgDetector.DetectShadows;
            set => _fgDetector.DetectShadows = value;
        }

        public int History
        {
            get => _fgDetector.History;
            set => _fgDetector.History = value;
        }

        public bool DetectForeground(UMat image, UMat output, int nFrames)
        {
            _fgDetector.Apply(image, output);
            if (nFrames < N_FRAMES_TO_LEARN)
            {
                return false;
            }

            else if (_regionOfInterest != null)
            {
                UMat fgMask = new UMat(UMatUsageFlags.DeviceMemory);
                Cv2.BitwiseAnd(output, _regionOfInterest, fgMask);
                fgMask.CopyTo(output);
                fgMask.Dispose();
            }

            return true;
        }

        public void SetRegionOfInterest(Mat roi)
        {
            if (roi != null)
            {
                _regionOfInterest = new UMat();
                roi.CopyTo(_regionOfInterest);
            }
        }
    }
}
