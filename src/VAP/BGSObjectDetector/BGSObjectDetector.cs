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
        private int MEDIAN_BLUR_SIZE = 5;
        private const int GAUSSIAN_BLUR_SIGMA = 7;
        private const int GAUSSIAN_BLUR_THRESHOLD = 53;
        private const int MIN_BLOB_SIZE = 70;
        private const int PRE_BGS_BLUR_SIGMA = 4;
        private FastGaussian _preGaussian;
        private FastGaussian _postGaussian;
        private static readonly SimpleBlobDetector.Params s_detectorParams = new SimpleBlobDetector.Params
        {
            //MinDistBetweenBlobs = 10, // 10 pixels between blobs
            //MinRepeatability = 1,

            //MinThreshold = GAUSSIAN_BLUR_THRESHOLD,
            //MaxThreshold = 200,
            ThresholdStep = 130,

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

        //private readonly FastBlobDetector _blobDetector;
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
            //_blobDetector = new FastBlobDetector(s_detectorParams);
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
            _preGaussian = new FastGaussian(PRE_BGS_BLUR_SIGMA, MatType.CV_32FC1);
            _postGaussian = new FastGaussian(GAUSSIAN_BLUR_SIGMA, MatType.CV_32FC1);
        }

        public bool DrawBoxes { get; set; }
        public Mat RegionOfInterest
        {
            get => _regionOfInterest;
            set => _regionOfInterest = value;
        }
        public List<IFramedItem> DetectObjects(DateTime timestamp, IFrame frame, object sourceObject)
        {
            if (_regionOfInterest != null)
                _bgs.SetRegionOfInterest(_regionOfInterest);

            var inputUMat = new UMat(UMatUsageFlags.DeviceMemory);
            frame.FrameData.CopyTo(inputUMat);

            var outputUMat = new UMat(UMatUsageFlags.DeviceMemory);

            {

                /*Cv2.MedianBlur(inputUMat, outputUMat, 3);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
                Cv2.MedianBlur(inputUMat, outputUMat, 3);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
                Cv2.MedianBlur(inputUMat, outputUMat, 3);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
                Cv2.MedianBlur(inputUMat, outputUMat, 3);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
                Cv2.GaussianBlur(frame.FrameData, outputUMat, OpenCvSharp.Size.Zero, PRE_BGS_BLUR_SIGMA);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);*/
                _preGaussian.Blur(ref inputUMat, ref outputUMat, frame.FrameData.Type());
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
            }

            {
                // fgMask is the original foreground bitmap returned by opencv MOG2
                if (!_bgs.DetectForeground(inputUMat, outputUMat, frame.FrameIndex))
                {
                    inputUMat.Dispose();
                    outputUMat.Dispose();
                    frame.ForegroundMask = null;
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
                _postGaussian.Blur(ref inputUMat, ref outputUMat, frame.FrameData.Type());
                (inputUMat, outputUMat) = (outputUMat, inputUMat);
            }

            {
                //Cv2.GaussianBlur(inputUMat, outputUMat, OpenCvSharp.Size.Zero, GAUSSIAN_BLUR_SIGMA);
                //(inputUMat, outputUMat) = (outputUMat, inputUMat);
            }

            {
                Cv2.Threshold(inputUMat, outputUMat, GAUSSIAN_BLUR_THRESHOLD, 255, ThresholdTypes.Binary);
                (inputUMat, outputUMat) = (outputUMat, inputUMat);/**/
            }
            (inputUMat, outputUMat) = (outputUMat, inputUMat);

            frame.ForegroundMask = new Mat();
            inputUMat.CopyTo(frame.ForegroundMask);
            /*Cv2.ImShow("FgMask", frame.ForegroundMask);
            Cv2.WaitKey(1);*/
            //CvBlobs blobs = new CvBlobs();
            KeyPoint[] points = _blobDetector.Detect(inputUMat);
            //IList<KeyPoint> points = new List<KeyPoint>();
            //_blobDetector.Detect(inputUMat, frame.ForegroundMask, ref points, null);
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
                    Cv2.Rectangle(frame.FrameData, new OpenCvSharp.Point(x - size, y - size), new OpenCvSharp.Point(x + size, y + size), new Scalar(255), 1);
                    Cv2.PutText(frame.FrameData, id.ToString(), new OpenCvSharp.Point(x, y - size), HersheyFonts.HersheyPlain, 1.0, new Scalar(255.0, 255.0, 255.0));
                }
            }
            if (DrawBoxes)
            {
                Cv2.PutText(frame.FrameData, "frame: " + frame.FrameIndex, new OpenCvSharp.Point(10, 10), HersheyFonts.HersheyPlain, 1, new Scalar(255, 255, 255));
            }

            return newBlobs;
        }
    }
}
