﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using BGSObjectDetector;
using OpenCvSharp;
using Utils;
using Utils.Items;
using Utils.ShapeTools;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace LineDetector
{
    public class DetectionLine
    {
        public static int MIN_BOX_SIZE = 1000;//smaller boxes than this will go.

        public static double DEFAULT_OCCUPANCY_THRESHOLD = 0.9; // default threhsold

        private LineSegment _lineSegment;

        public LineSegment Line
        {
            get => _lineSegment;
            set => _lineSegment = value;
        }

        /// <summary>
        /// The first point of the line.
        /// </summary>
        public Point P1
        {
            get => _lineSegment.P1;
            set => _lineSegment.P1 = value;
        }

        /// <summary>
        /// The second point of the line.
        /// </summary>
        public Point P2
        {
            get => _lineSegment.P2;
            set => _lineSegment.P2 = value;
        }

        public string LineName { get; set; }

        /// <summary>
        /// The step size used to determine occupancy.
        /// </summary>
        public double Increment { get; set; }

        /// <summary>
        /// The overlap threshold used to determine occupancy.
        /// </summary>
        public double OverlapFractionThreshold { get; set; } = DEFAULT_OCCUPANCY_THRESHOLD;

        /// <inheritdoc cref="DetectionLine(int, int, int, int, double)"/>
        public DetectionLine(int a, int b, int c, int d, string lineName)
        {
            P1 = new Point(a, b);
            P2 = new Point(c, d);
            LineName = lineName;
            double length = _lineSegment.Length;
            Increment = 1 / (2 * length);
        }

        /// <summary>
        /// Creates a <see cref="DetectionLine"/> using the given coordinates.
        /// </summary>
        /// <param name="a">The X coordinate of the first point of the line.</param>
        /// <param name="b">The Y coordinate of the first point of the line.</param>
        /// <param name="c">The X coordinate of the second point of the line.</param>
        /// <param name="d">The Y coordinate of the second point of the line.</param>
        /// <param name="l_threshold">The overlap acceptance threshold used for a positive detection.</param>
        public DetectionLine(int a, int b, int c, int d, double l_threshold, string lineName)
        {
            P1 = new Point(a, b);
            P2 = new Point(c, d);
            LineName = lineName;
            double length = _lineSegment.Length;
            Increment = 1 / (2 * length);
            OverlapFractionThreshold = l_threshold;
        }

        /// <summary>
        /// Calculates the fraction of the <see cref="DetectionLine"/> that overlaps the given mask AND the given box.
        /// </summary>
        /// <param name="b">The bounding box of the area of interest in the mask.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>
        /// Returns the fraction of this DetectionLine that overlaps
        /// the given mask, from 0 indicating no overlap to 1 indicating
        /// complete overlap.
        /// </returns>
        public double GetFractionContainedInBox(Bitmap mask)
        {
            double eta = 0;
            Size size = Line.P2Offset;
            double currentX = P1.X + eta * size.Width;
            double currentY = P1.Y + eta * size.Height;
            double lastX = -1;
            double lastY = -1;
            int totalPixelCount = 0;
            int overlapCount = 0;

            do
            {
                if ((lastX == currentX) && (lastY == currentY)) continue;

                totalPixelCount++;

                // bool isInside = b.Contains((int)currentX, (int)currentY);

                if (mask.GetPixel((int)currentX, (int)currentY).ToArgb() == (~0))
                {
                    overlapCount++;
                }

                lastX = currentX; lastY = currentY;
                eta += Increment;
                currentX = P1.X + eta * size.Width;
                currentY = P1.Y + eta * size.Height;
            } while (eta <= 1);

            double fraction = (double)overlapCount / (double)totalPixelCount;
            return fraction;
        }

        /// <summary>
        /// Calculates the fraction of the <see cref="DetectionLine"/> that overlaps the given mask AND the given box.
        /// </summary>
        /// <param name="b">The bounding box of the area of interest in the mask.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>
        /// Returns the fraction of this DetectionLine that overlaps
        /// the given mask, from 0 indicating no overlap to 1 indicating
        /// complete overlap.
        /// </returns>
        public double GetFractionContainedInBox(OpenCvSharp.Mat mask)
        {
            double eta = 0;
            Size size = Line.P2Offset;
            double currentX = P1.X + eta * size.Width;
            double currentY = P1.Y + eta * size.Height;
            double lastX = -1;
            double lastY = -1;
            int totalPixelCount = 0;
            int overlapCount = 0;

            do
            {
                if ((lastX == currentX) && (lastY == currentY))
                    continue;

                totalPixelCount++;

                // bool isInside = b.Contains((int)currentX, (int)currentY);
                byte v = mask.Get<byte>((int)currentY, (int)currentX);
                if (v == 255)
                {
                    overlapCount++;
                }

                lastX = currentX;
                lastY = currentY;
                eta += Increment;
                currentX = P1.X + eta * size.Width;
                currentY = P1.Y + eta * size.Height;
            } while (eta <= 1);

            double fraction = (double)overlapCount / (double)totalPixelCount;
            return fraction;
        }

        /// <summary>
        /// Calculates the fraction of the <see cref="DetectionLine"/> which overlaps the given mask.
        /// </summary>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>
        /// Returns the fraction of this DetectionLine that overlaps
        /// the given mask, from 0 indicating no overlap to 1 indicating
        /// complete overlap.
        /// </returns>
        public double GetFractionInForeground(Bitmap mask)
        {
            double eta = 0;
            Size size = Line.P2Offset;
            double currentX = P1.X + eta * size.Width;
            double currentY = P1.Y + eta * size.Height;
            double lastX = -1;
            double lastY = -1;
            int totalPixelCount = 0;
            int overlapCount = 0;

            do
            {
                if ((lastX == currentX) && (lastY == currentY)) continue;

                totalPixelCount++;

                if (mask.GetPixel((int)currentX, (int)currentY).ToString() == "Color [A=255, R=255, G=255, B=255]")
                {
                    overlapCount++;
                }

                lastX = currentX; lastY = currentY;
                eta += Increment;
                currentX = P1.X + eta * size.Width;
                currentY = P1.Y + eta * size.Height;
            } while (eta <= 1);

            double fraction = (double)overlapCount / (double)totalPixelCount;
            return fraction;
        }

        public double GetFractionInForeground(Mat mask)
        {
            double eta = 0;
            Size size = Line.P2Offset;
            double currentX = P1.X + eta * size.Width;
            double currentY = P1.Y + eta * size.Height;
            double lastX = -1;
            double lastY = -1;
            int totalPixelCount = 0;
            int overlapCount = 0;

            do
            {
                if ((lastX == currentX) && (lastY == currentY))
                    continue;

                totalPixelCount++;

                /**/
                if (mask.Get<byte>((int)currentY, (int)currentX) == 255)
                {
                    overlapCount++;
                }

                lastX = currentX;
                lastY = currentY;
                eta += Increment;
                currentX = P1.X + eta * size.Width;
                currentY = P1.Y + eta * size.Height;
            } while (eta <= 1);

            double fraction = (double)overlapCount / (double)totalPixelCount;
            return fraction;
        }

        /// <summary>
        /// Finds the box with the maximum overlap fraction with this <see cref="DetectionLine"/>.
        /// </summary>
        /// <param name="boxes">The list of <see cref="Box"/> objects to check, representing the bounding boxes of items in frame.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>Returns a tuple containing both the maximum overlap fraction found, and the <see cref="Box"/> associated with that overlap.</returns>
        public (double frac, IFramedItem b) GetMaximumFractionContainedInAnyBox(IList<IFramedItem> boxes, Bitmap mask)
        {
            double maxOverlapFraction = 0;
            IFramedItem maxB = null;
            for (int boxNo = 0; boxNo < boxes.Count; boxNo++)
            {
                IFramedItem b = boxes[boxNo];
                StatisticRectangle sr = new StatisticRectangle(from item in b.ItemIDs select item.BoundingBox);
                double area = sr.Mean.Width * sr.Mean.Height;
                if (area < MIN_BOX_SIZE) continue;
                double overlapFraction = GetFractionContainedInBox(mask);
                if (overlapFraction > maxOverlapFraction)
                {
                    maxOverlapFraction = overlapFraction;
                    maxB = b;
                }
            }
            return (maxOverlapFraction, maxB);
        }

        /// <summary>
        /// Finds the box with the maximum overlap fraction with this <see cref="DetectionLine"/>.
        /// </summary>
        /// <param name="boxes">The list of <see cref="Box"/> objects to check, representing the bounding boxes of items in frame.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>Returns a tuple containing both the maximum overlap fraction found, and the <see cref="Box"/> associated with that overlap.</returns>
        public (double frac, IFramedItem b) GetMaximumFractionContainedInAnyBox(IList<IFramedItem> boxes, OpenCvSharp.Mat mask)
        {
            double maxOverlapFraction = 0;
            IFramedItem maxB = null;
            for (int boxNo = 0; boxNo < boxes.Count; boxNo++)
            {
                IFramedItem b = boxes[boxNo];
                StatisticRectangle sr = new StatisticRectangle(b.ItemIDs);
                double area = sr.Mean.Width * sr.Mean.Height;
                if (area < MIN_BOX_SIZE)
                    continue;
                double overlapFraction;
                if(b.Frame.ForegroundMask != null)
                {
                    overlapFraction = GetFractionContainedInBox(mask);
                }
                else
                {
                    overlapFraction = Utils.LineOverlapFilter.GetOverlapRatio(this.Line, b.ItemIDs.Last().BoundingBox);
                }

                if (overlapFraction > maxOverlapFraction)
                {
                    maxOverlapFraction = overlapFraction;
                    maxB = b;
                }
            }
            return (maxOverlapFraction, maxB);
        }

        /// <summary>
        /// Determines if this line is occupied.
        /// </summary>
        /// <param name="boxes">The bounding boxes of items in the frame.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>
        /// Returns a tuple containing a boolean indicating whether this line is
        /// occupied, and the bounding box of the occupying item if so. If this line is
        /// unoccupied, the bounding box will be null.
        /// </returns>
        public (bool occupied, IFramedItem box) IsOccupied(IList<IFramedItem> boxes, Bitmap mask)
        {
            (double frac, IFramedItem b) = GetMaximumFractionContainedInAnyBox(boxes, mask);
            if (frac >= OverlapFractionThreshold)
            {
                IItemID existingID = b.ItemIDs.Last();
                ILineTriggeredItemID id = new LineTriggeredItemID(existingID.BoundingBox, existingID.ObjectID, existingID.ObjName, existingID.Confidence, existingID.TrackID, nameof(DetectionLine))
                {
                    TriggerSegment = this.Line,
                    TriggerLine = LineName
                };
                b.ItemIDs.Add(id);

                return (true, b);
            }
            else
            {
                return (false, null);
            }
        }

        /// <summary>
        /// Determines if this line is occupied.
        /// </summary>
        /// <param name="boxes">The bounding boxes of items in the frame.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>
        /// Returns a tuple containing a boolean indicating whether this line is
        /// occupied, and the bounding box of the occupying item if so. If this line is
        /// unoccupied, the bounding box will be null.
        /// </returns>
        public (bool occupied, IFramedItem box) IsOccupied(IList<IFramedItem> boxes, OpenCvSharp.Mat mask)
        {
            return IsOccupied(boxes, mask, null);
        }

        /// <summary>
        /// Determines if this line is occupied.
        /// </summary>
        /// <param name="boxes">The bounding boxes of items in the frame.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>
        /// Returns a tuple containing a boolean indicating whether this line is
        /// occupied, and the bounding box of the occupying item if so. If this line is
        /// unoccupied, the bounding box will be null.
        /// </returns>
        public (bool occupied, IFramedItem box) IsOccupied(IList<IFramedItem> boxes, OpenCvSharp.Mat mask, object signature)
        {
            (double frac, IFramedItem b) = GetMaximumFractionContainedInAnyBox(boxes, mask);
            if (frac >= OverlapFractionThreshold)
            {
                IItemID existingID = b.ItemIDs.Last();
                ILineTriggeredItemID id = new LineTriggeredItemID(existingID.BoundingBox, existingID.ObjectID, existingID.ObjName, existingID.Confidence, existingID.TrackID, nameof(DetectionLine))
                {
                    TriggerSegment = this.Line,
                    TriggerLine = LineName,
                    SourceObject = signature,
                    FurtherAnalysisTriggered = true
                };
                b.ItemIDs.Add(id);
                return (true, b);
            }
            else
            {
                return (false, null);
            }
        }

        /// <summary>
        /// Determines if this line is occupied.
        /// </summary>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>Returns true if the line is occupied, and false otherwise.</returns>
        public bool IsOccupied(Bitmap mask)
        {
            double frac = GetFractionInForeground(mask);
            if (frac >= OverlapFractionThreshold)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RotateLine(int rotateCount, Size frameSize)
        {
            rotateCount = rotateCount & 3;
            if(rotateCount == 1)
            {
                var p1 = new Point(frameSize.Height - P1.Y, P1.X);
                var p2 = new Point(frameSize.Height - P2.Y, P2.X);
                _lineSegment = new LineSegment(p1, p2);
            }
            else if (rotateCount == 2)
            {
                var p1 = new Point(frameSize.Width - P1.X, frameSize.Height - P1.Y);
                var p2 = new Point(frameSize.Width - P2.X, frameSize.Height - P2.Y);
                _lineSegment = new LineSegment(p1, p2);
            }
            else if (rotateCount == 3)
            {
                var p1 = new Point(P1.Y, frameSize.Width - P1.X);
                var p2 = new Point(P2.Y, frameSize.Width - P2.X);

                _lineSegment = new LineSegment(p1, p2);
            }
        }

        public override string ToString()
        {
            return P1.X + "\t" + P1.Y + "\t" + P2.X + "\t" + P2.Y + "\t" + OverlapFractionThreshold;
        }
    }
}
