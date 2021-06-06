﻿// Copyright (c) Microsoft Corporation.
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
        private MOG2 bgs;

        private Mat blurredFrame = new Mat();
        private Mat fgMask = new Mat();
        private Mat fgWOShadows = new Mat();
        private Mat fgSmoothedMask2 = new Mat();
        private Mat fgSmoothedMask3 = new Mat();
        private Mat fgSmoothedMask4 = new Mat();

        private Mat regionOfInterest = null;

        private int PRE_BGS_BLUR_SIGMA = 2;
        private int MEDIAN_BLUR_SIZE = 5;
        private int GAUSSIAN_BLUR_SIGMA = 4;
        private int GAUSSIAN_BLUR_THRESHOLD = 50;
        private static int MIN_BLOB_SIZE = 30;

        private static SimpleBlobDetector.Params detectorParams = new SimpleBlobDetector.Params
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
        private SimpleBlobDetector _blobDetector = SimpleBlobDetector.Create(detectorParams);

        //public BGSObjectDetector(MOG2 bgs)
        public BGSObjectDetector()
        {
            //this.bgs = bgs;
            bgs = new MOG2();
        }

        public IList<IFramedItem> DetectObjects(DateTime timestamp, Mat image, int frameIndex, out Mat fg)
        {
            if (regionOfInterest != null)
                bgs.SetRegionOfInterest(regionOfInterest);

            Cv2.GaussianBlur(image, blurredFrame, OpenCvSharp.Size.Zero, PRE_BGS_BLUR_SIGMA);

            // fgMask is the original foreground bitmap returned by opencv MOG2
            fgMask = bgs.DetectForeground(blurredFrame, frameIndex);
            fg = fgMask;
            if (fgMask == null)
                return null;

            // pre-processing
            Cv2.Threshold(fgMask, fgWOShadows, 200, 255, ThresholdTypes.Binary);
            Cv2.MedianBlur(fgWOShadows, fgSmoothedMask2, MEDIAN_BLUR_SIZE);
            Cv2.GaussianBlur(fgSmoothedMask2, fgSmoothedMask3, OpenCvSharp.Size.Zero, GAUSSIAN_BLUR_SIGMA);
            Cv2.Threshold(fgSmoothedMask3, fgSmoothedMask4, GAUSSIAN_BLUR_THRESHOLD, 255, ThresholdTypes.Binary);

            fg = fgSmoothedMask4;

            //CvBlobs blobs = new CvBlobs();
            KeyPoint[] points = _blobDetector.Detect(fgSmoothedMask4);
            IFrame frame = new Frame("", frameIndex);
            frame.TimeStamp = timestamp;

            frame.FrameData = image;
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

                Box box = new Box("", x - size, x + size, y - size, y + size, frameIndex, id);
                IItemID itemID = new ItemID(new Rectangle(box.X0, box.Y0, box.Width, box.Height), 0, null, 0, (int)id, nameof(BGSObjectDetector));
                IFramedItem item = new FramedItem(frame, itemID);
                id++;
                newBlobs.Add(item);

                Cv2.Rectangle(fgSmoothedMask4, new OpenCvSharp.Point(x - size, y - size), new OpenCvSharp.Point(x + size, y + size), new Scalar(255), 1);
                Cv2.PutText(fgSmoothedMask4, box.ID.ToString(), new OpenCvSharp.Point(x, y - size), HersheyFonts.HersheyPlain, 1.0, new Scalar(255.0, 255.0, 255.0));
            }
            Cv2.PutText(fgSmoothedMask4, "frame: " + frameIndex, new OpenCvSharp.Point(10, 10), HersheyFonts.HersheyPlain, 1, new Scalar(255, 255, 255));

            return newBlobs;
        }
    }
}
