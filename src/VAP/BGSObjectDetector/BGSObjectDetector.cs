// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

﻿using System;
using System.Collections.Generic;

using OpenCvSharp;
//using OpenCvSharp.Blob;

namespace BGSObjectDetector
{
    public class BGSObjectDetector
    {
        MOG2 bgs;


        Mat regionOfInterest = null;

        int PRE_BGS_BLUR_SIGMA = 2;
        int MEDIAN_BLUR_SIZE = 5;
        int GAUSSIAN_BLUR_SIGMA = 4;
        int GAUSSIAN_BLUR_THRESHOLD = 50;
        static int MIN_BLOB_SIZE = 30;

        static SimpleBlobDetector.Params detectorParams = new SimpleBlobDetector.Params
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
        SimpleBlobDetector _blobDetector = SimpleBlobDetector.Create(detectorParams);

        //public BGSObjectDetector(MOG2 bgs)
        public BGSObjectDetector()
        {
            //this.bgs = bgs;
            bgs = new MOG2();
        }

        public List<Box> DetectObjects(DateTime timestamp, Mat image, int frameIndex, out Mat fg)
        {
            if (regionOfInterest != null)
                bgs.SetRegionOfInterest(regionOfInterest);

            var inputUMat = new UMat(UMatUsageFlags.DeviceMemory);
            image.CopyTo(inputUMat);

            {
                var blurredFrame = new UMat(UMatUsageFlags.DeviceMemory);
                Cv2.GaussianBlur(image, blurredFrame, Size.Zero, PRE_BGS_BLUR_SIGMA);
                ExchangeAndRelease(ref inputUMat, ref blurredFrame);
            }

            {
                // fgMask is the original foreground bitmap returned by opencv MOG2
                UMat fgMask = bgs.DetectForeground(inputUMat, frameIndex);
                if (fgMask == null)
                {
                    fg = null;
                    return null;
                }

                ExchangeAndRelease(ref inputUMat, ref fgMask);
            }

            // pre-processing
            {
                UMat fgWOShadows = new UMat(UMatUsageFlags.DeviceMemory);
                Cv2.Threshold(inputUMat, fgWOShadows, 200, 255, ThresholdTypes.Binary);
                ExchangeAndRelease(ref inputUMat, ref fgWOShadows);
            }

            {
                UMat fgSmoothedMask2 = new UMat(UMatUsageFlags.DeviceMemory);
                Cv2.MedianBlur(inputUMat, fgSmoothedMask2, MEDIAN_BLUR_SIZE);
                ExchangeAndRelease(ref inputUMat, ref fgSmoothedMask2);
            }

            {
                UMat fgSmoothedMask3 = new UMat(UMatUsageFlags.DeviceMemory);
                Cv2.GaussianBlur(inputUMat, fgSmoothedMask3, Size.Zero, GAUSSIAN_BLUR_SIGMA);
                ExchangeAndRelease(ref inputUMat, ref fgSmoothedMask3);
            }

            {
                UMat fgSmoothedMask4 = new UMat(UMatUsageFlags.DeviceMemory);
                Cv2.Threshold(inputUMat, fgSmoothedMask4, GAUSSIAN_BLUR_THRESHOLD, 255, ThresholdTypes.Binary);
                ExchangeAndRelease(ref inputUMat, ref fgSmoothedMask4);
            }

            fg = new Mat();
            inputUMat.CopyTo(fg);

            //CvBlobs blobs = new CvBlobs();
            KeyPoint[] points = _blobDetector.Detect(inputUMat);
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
                Box box = new Box("", x - size, x + size, y - size, y + size, frameIndex, id);
                id++;
                newBlobs.Add(box);

                Cv2.Rectangle(image, new OpenCvSharp.Point(x - size, y - size), new OpenCvSharp.Point(x + size, y + size), new Scalar(255), 1);
                Cv2.PutText(image, box.ID.ToString(), new OpenCvSharp.Point(x, y - size), HersheyFonts.HersheyPlain, 1.0, new Scalar(255.0, 255.0, 255.0));
            }
            Cv2.PutText(image, "frame: " + frameIndex, new OpenCvSharp.Point(10, 10), HersheyFonts.HersheyPlain, 1, new Scalar(255, 255, 255));

            newBlobs.ForEach(b => b.Time = timestamp);
            newBlobs.ForEach(b => b.Timestamp = frameIndex);
            return newBlobs;

            void ExchangeAndRelease(ref UMat priorInput, ref UMat output)
            {
                priorInput?.Dispose();
                priorInput = output;
                output = null;
            }
        }
    }
}
