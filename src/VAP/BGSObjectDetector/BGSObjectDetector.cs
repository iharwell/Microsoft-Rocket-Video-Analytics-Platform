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
        private readonly MOG2 _bgs;

        private readonly Mat _blurredFrame = new Mat();
        private Mat _fgMask = new Mat();
        private readonly Mat _fgWOShadows = new Mat();
        private readonly Mat _fgSmoothedMask2 = new Mat();
        private readonly Mat _fgSmoothedMask3 = new Mat();
        // private readonly Mat _fgSmoothedMask4 = new Mat();

        private readonly Mat _kernel;

        private readonly Mat _regionOfInterest = null;

        private const int PRE_BGS_BLUR_SIGMA = 4;
        //private int MEDIAN_BLUR_SIZE = 5;
        private const int GAUSSIAN_BLUR_SIGMA = 5;
        private const int GAUSSIAN_BLUR_THRESHOLD = 60;
        private const int MIN_BLOB_SIZE = 80;

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
        private readonly SimpleBlobDetector _blobDetector = SimpleBlobDetector.Create(s_detectorParams);

        static BGSObjectDetector()
        {
            Cv2.SetUseOptimized(true);
        }

        //public BGSObjectDetector(MOG2 bgs)
        public BGSObjectDetector()
        {
            //this.bgs = bgs;
            _bgs = new MOG2();
            _kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(5, 5));
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

            // pre-processing
            Cv2.Threshold(_fgMask, _fgWOShadows, 215, 255, ThresholdTypes.Binary);
            //Cv2.MedianBlur(fgWOShadows, fgSmoothedMask2, MEDIAN_BLUR_SIZE);
            Cv2.MorphologyEx(_fgWOShadows, _fgSmoothedMask2, MorphTypes.Erode, _kernel);
            Cv2.MorphologyEx(_fgSmoothedMask2, _fgWOShadows, MorphTypes.Open, _kernel);
            Cv2.MorphologyEx(_fgWOShadows, _fgSmoothedMask2, MorphTypes.Dilate, _kernel);
            //Cv2.GaussianBlur(_fgWOShadows, _fgSmoothedMask2, OpenCvSharp.Size.Zero, 4);
            //Cv2.GaussianBlur(_fgSmoothedMask2, _fgWOShadows, OpenCvSharp.Size.Zero, 4);
            Cv2.GaussianBlur(_fgSmoothedMask2, _fgWOShadows, OpenCvSharp.Size.Zero, 4);
            //Cv2.GaussianBlur(_fgSmoothedMask2, _fgWOShadows, OpenCvSharp.Size.Zero, GAUSSIAN_BLUR_SIGMA*2);
            //Cv2.MedianBlur(_fgSmoothedMask2, _fgWOShadows, MEDIAN_BLUR_SIZE);
            Cv2.Threshold(_fgWOShadows, _fgSmoothedMask3, GAUSSIAN_BLUR_THRESHOLD, 255, ThresholdTypes.Binary);

            fg = _fgSmoothedMask3;

            //CvBlobs blobs = new CvBlobs();
            KeyPoint[] points = _blobDetector.Detect(_fgSmoothedMask3);
            IFrame frame = new Frame(null, frameIndex)
            {
                TimeStamp = timestamp,
                FrameData = image
            };
            //frame.FrameData = Utils.Utils.ImageToByteBmp( OpenCvSharp.Extensions.BitmapConverter.ToBitmap( image ) );
            //blobs.FilterByArea(MIN_BLOB_SIZE, int.MaxValue);

            //// filter overlapping blobs
            //HashSet<uint> blobIdsToRemove = new HashSet<uint>();
            //foreach (var b0 in blobs)
            //    foreach (var b1 in blobs)
            //    {
            //        if (b0.Key == b1.Key) continue;
            //        if (b0.Value.BoundingBox.Contains(b1.Value.BoundingBox))
            //            blobIdsToRemove.Add(b1.Key);
            //    }
            //foreach (uint blobid in blobIdsToRemove)
            //    blobs.Remove(blobid);

            // adding text to boxes and foreground frame
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

                Cv2.Rectangle(_fgSmoothedMask3, new OpenCvSharp.Point(x - size, y - size), new OpenCvSharp.Point(x + size, y + size), new Scalar(255), 1);
                Cv2.PutText(_fgSmoothedMask3, id.ToString(), new OpenCvSharp.Point(x, y - size), HersheyFonts.HersheyPlain, 1.0, new Scalar(255.0));
            }
            Cv2.PutText(_fgSmoothedMask3, "frame: " + frameIndex, new OpenCvSharp.Point(10, 10), HersheyFonts.HersheyPlain, 1, new Scalar(255));

            return newBlobs;
        }
    }
}
