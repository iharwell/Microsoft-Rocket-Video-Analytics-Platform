// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using OpenCvSharp;

namespace BGSObjectDetector
{
    internal class MOG2
    {
        private const int N_FRAMES_TO_LEARN = 60;
        private readonly BackgroundSubtractorMOG2 _fgDetector = BackgroundSubtractorMOG2.Create(250, 20); //try sweeping (also set it higher than 25)
        private readonly Mat _fgMask = new Mat();
        private readonly Mat _fgMask0 = new Mat();
        private Mat _regionOfInterest = null;
        // Why do we need this?

        public Mat RegionOfInterest
        {
            get => _regionOfInterest;
            set => _regionOfInterest = value;
        }

        public MOG2()
        {
            _regionOfInterest = null;
            _fgMask0 = new Mat();
            _fgMask = new Mat();
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

        public Mat DetectForeground(Mat image, int nFrames)
        {
            _fgDetector.Apply(image, _fgMask0);

            if (_regionOfInterest != null)
                Cv2.BitwiseAnd(_fgMask0, _regionOfInterest, _fgMask);

            if (nFrames < N_FRAMES_TO_LEARN)
                return null;
            else if (_regionOfInterest != null)
                return _fgMask;
            else
                return _fgMask0;
        }

        public void SetRegionOfInterest(Mat roi)
        {
            if (roi != null)
            {
                _regionOfInterest = new Mat();
                roi.CopyTo(_regionOfInterest);
            }
        }
    }
}
