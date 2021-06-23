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
        public List<Box> DetectObjects(DateTime timestamp, Mat image, int frameIndex, out Mat fg)
        {
            if (regionOfInterest != null)
                bgs.SetRegionOfInterest(regionOfInterest);

            var inputUMat = new UMat(UMatUsageFlags.DeviceMemory);
            image.CopyTo(inputUMat);

            var outputUMat = new UMat(UMatUsageFlags.DeviceMemory);

            {
                Cv2.GaussianBlur(image, outputUMat, Size.Zero, PRE_BGS_BLUR_SIGMA);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
            }

            {
                // fgMask is the original foreground bitmap returned by opencv MOG2
                if (!bgs.DetectForeground(inputUMat, outputUMat, frameIndex))
                {
                    inputUMat.Dispose();
                    outputUMat.Dispose();
                    fg = null;
                    return null;
                }

                (inputUMat, outputUMat) = (outputUMat, inputUMat);
            }

            // pre-processing
            {
                Cv2.Threshold(inputUMat, outputUMat, 200, 255, ThresholdTypes.Binary);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
            }

            {
                Cv2.MedianBlur(inputUMat, outputUMat, MEDIAN_BLUR_SIZE);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
            }

            {
                Cv2.GaussianBlur(inputUMat, outputUMat, Size.Zero, GAUSSIAN_BLUR_SIGMA);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
            }

            {
                Cv2.Threshold(inputUMat, outputUMat, GAUSSIAN_BLUR_THRESHOLD, 255, ThresholdTypes.Binary);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
            }

            fg = new Mat();
            inputUMat.CopyTo(fg);

            //CvBlobs blobs = new CvBlobs();
            KeyPoint[] points = _blobDetector.Detect(inputUMat);
            outputUMat.Dispose();
            inputUMat.Dispose();
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
            List<Box> newBlobs = new List<Box>();
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
                Cv2.Rectangle(image, new OpenCvSharp.Point(x - size, y - size), new OpenCvSharp.Point(x + size, y + size), new Scalar(255), 1);
                Cv2.PutText(image, box.ID.ToString(), new OpenCvSharp.Point(x, y - size), HersheyFonts.HersheyPlain, 1.0, new Scalar(255.0, 255.0, 255.0));
                }
            }
            if (DrawBoxes)
            {
			    Cv2.PutText(image, "frame: " + frameIndex, new OpenCvSharp.Point(10, 10), HersheyFonts.HersheyPlain, 1, new Scalar(255, 255, 255));
            }

            return newBlobs;
        }
    }
}
