// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenCvSharp;
using Utils.Items;

namespace BGSObjectDetector
{
    public class BGSObjectDetector
    {
        //private int MEDIAN_BLUR_SIZE = 5;
        private const int GAUSSIAN_BLUR_SIGMA = 5;
        private const int GAUSSIAN_BLUR_THRESHOLD = 53;
        private const int MIN_BLOB_SIZE = 70;
        private const int PRE_BGS_BLUR_SIGMA = 4;

        private static readonly SimpleBlobDetector.Params s_detectorParams = new SimpleBlobDetector.Params
        {
            //MinDistBetweenBlobs = 10, // 10 pixels between blobs
            //MinRepeatability = 1,

            //MinThreshold = 100,
            //MaxThreshold = 255,
            //ThresholdStep = 5,

            FilterByArea = true,
            MinArea = MIN_BLOB_SIZE,
            MaxArea = int.MaxValue,

            FilterByCircularity = false,
            //FilterByCircularity = true,
            //MinCircularity = 0.001f,

            FilterByConvexity = false,
            //FilterByConvexity = true,
            //MinConvexity = 0.001f,
            //MaxConvexity = 10,

            FilterByInertia = false,
            //FilterByInertia = true,
            //MinInertiaRatio = 0.001f,

            FilterByColor = false
            //FilterByColor = true,
            //BlobColor = 255 // to extract light blobs
        };

        private readonly MOG2 _bgs;

        private readonly SimpleBlobDetector _blobDetector;
        private readonly Mat _blurredFrame;
        private readonly Mat _fgSmoothedMask2;
        private readonly Mat _fgSmoothedMask3;
        private readonly Mat _fgWOShadows;
        private readonly Mat _kernel2;
        private readonly Mat _kernel5;
        private Mat _regionOfInterest;
        private Mat _fgMask;

        static BGSObjectDetector()
        {
            Cv2.SetUseOptimized(true);
        }

        //public BGSObjectDetector(MOG2 bgs)
        public BGSObjectDetector()
        {

            _blobDetector = SimpleBlobDetector.Create(s_detectorParams);
            //this.bgs = bgs;
            _bgs = new MOG2();
            _blurredFrame = new Mat();
            _fgSmoothedMask2 = new Mat();
            _fgSmoothedMask3 = new Mat();
            _fgWOShadows = new Mat();
            _kernel2 = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(2, 2));
            _kernel5 = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(5, 5));
            _regionOfInterest = null;
            _fgMask = new Mat();
        }

        public bool DrawBoxes { get; set; }
        public Mat RegionOfInterest
        {
            get => _regionOfInterest;
            set => _regionOfInterest = value;
        }

        public IList<IFramedItem> DetectObjects(DateTime timestamp, Mat image, int frameIndex, out Mat fg, object sourceObject = null)
        {
            if (_regionOfInterest != null)
                _bgs.SetRegionOfInterest(_regionOfInterest);

            Cv2.GaussianBlur(image, _blurredFrame, OpenCvSharp.Size.Zero, PRE_BGS_BLUR_SIGMA);

            // fgMask is the original foreground bitmap returned by opencv MOG2
            _fgMask = _bgs.DetectForeground(_blurredFrame, frameIndex);
            // fg = _fgMask;
            if (_fgMask == null)
            {
                fg = null;
                return null;
            }
            Cv2.MorphologyEx(_fgMask, _fgSmoothedMask2, MorphTypes.Open, _kernel2);
            Cv2.MorphologyEx(_fgSmoothedMask2, _fgWOShadows, MorphTypes.Erode, _kernel2);
            Cv2.MorphologyEx(_fgWOShadows, _fgSmoothedMask2, MorphTypes.Close, _kernel5);
            // pre-processing
            //Cv2.Threshold(_fgMask, _fgWOShadows, 210, 255, ThresholdTypes.Binary);
            //Cv2.GaussianBlur(_fgSmoothedMask2, _fgWOShadows, OpenCvSharp.Size.Zero, 4);
            //Cv2.Threshold(_fgWOShadows, _fgSmoothedMask2, GAUSSIAN_BLUR_THRESHOLD, 255, ThresholdTypes.Binary);
            Cv2.MorphologyEx(_fgSmoothedMask2, _fgSmoothedMask3, MorphTypes.Dilate, _kernel5);

            fg = _fgSmoothedMask3;

            KeyPoint[] points = _blobDetector.Detect(_fgSmoothedMask3);
            IFrame frame = new Frame(null, frameIndex)
            {
                TimeStamp = timestamp,
                FrameData = image
            };
            List<IFramedItem> newBlobs = new List<IFramedItem>();
            uint id = 0;
            foreach (var point in points)
            {
                int x = (int)point.Pt.X;
                int y = (int)point.Pt.Y;
                int size = (int)point.Size;

                //Box box = new Box("", x - size, x + size, y - size, y + size, frameIndex, id);

                Rectangle r = new Rectangle(x - size, y - size, size * 2, size * 2);
                IItemID itemID = new ItemID(r, 0, null, 0, (int)id, nameof(BGSObjectDetector));
                IFramedItem item = new FramedItem(frame, itemID);
                itemID.SourceObject = sourceObject;
                id++;
                newBlobs.Add(item);
                if (DrawBoxes)
                {
                    Cv2.Rectangle(_fgSmoothedMask3, new OpenCvSharp.Point(x - size, y - size), new OpenCvSharp.Point(x + size, y + size), new Scalar(255), 1);
                    Cv2.PutText(_fgSmoothedMask3, id.ToString(), new OpenCvSharp.Point(x, y - size), HersheyFonts.HersheyPlain, 1.0, new Scalar(255.0));
                }
            }
            if (DrawBoxes)
            {
                Cv2.PutText(_fgSmoothedMask3, "frame: " + frameIndex, new OpenCvSharp.Point(10, 10), HersheyFonts.HersheyPlain, 1, new Scalar(255));
            }

            return newBlobs;
        }
    }
}
