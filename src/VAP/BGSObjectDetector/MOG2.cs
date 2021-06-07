// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using OpenCvSharp;

namespace BGSObjectDetector
{
    internal class MOG2
    {
        private readonly BackgroundSubtractorMOG2 _fgDetector = BackgroundSubtractorMOG2.Create(500, 10); //try sweeping (also set it higher than 25)
        private Mat _regionOfInterest = null;
        private readonly Mat _fgMask0 = new Mat();
        private readonly Mat _fgMask = new Mat();

        private const int N_FRAMES_TO_LEARN = 120; // Why do we need this?

        public MOG2()
        {
            _regionOfInterest = null;
            _fgMask0 = new Mat();
            _fgMask = new Mat();
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
