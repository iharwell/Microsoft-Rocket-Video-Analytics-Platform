// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BGSObjectDetector;
using Utils;
using Utils.Items;
using Utils.ShapeTools;

namespace LineDetector
{
    public class DetectionLine
    {
        public static int MIN_BOX_SIZE = 1000;//smaller boxes than this will go.

        public static double DEFAULT_OCCUPANCY_THRESHOLD = 0.9; // default threhsold

        LineSegment lineSegment;

        public LineSegment Line
        {
            get => lineSegment;
            set => lineSegment = value;
        }

        /// <summary>
        /// The first point of the line.
        /// </summary>
        public Point p1
        {
            get => lineSegment.P1;
            set => lineSegment.P1 = value;
        }

        /// <summary>
        /// The second point of the line.
        /// </summary>
        public Point p2
        {
            get => lineSegment.P2;
            set => lineSegment.P2 = value;
        }

        /// <summary>
        /// The step size used to determine occupancy.
        /// </summary>
        public double increment { get; set; }

        /// <summary>
        /// The overlap threshold used to determine occupancy.
        /// </summary>
        public double overlapFractionThreshold { get; set; } = DEFAULT_OCCUPANCY_THRESHOLD;

        /// <inheritdoc cref="DetectionLine(int, int, int, int, double)"/>
        public DetectionLine(int a, int b, int c, int d)
        {
            p1 = new Point(a, b);
            p2 = new Point(c, d);
            double length = lineSegment.Length;
            increment = 1 / (2 * length);
        }

        /// <summary>
        /// Creates a <see cref="DetectionLine"/> using the given coordinates.
        /// </summary>
        /// <param name="a">The X coordinate of the first point of the line.</param>
        /// <param name="b">The Y coordinate of the first point of the line.</param>
        /// <param name="c">The X coordinate of the second point of the line.</param>
        /// <param name="d">The Y coordinate of the second point of the line.</param>
        /// <param name="l_threshold">The overlap acceptance threshold used for a positive detection.</param>
        public DetectionLine(int a, int b, int c, int d, double l_threshold)
        {
            p1 = new Point(a, b);
            p2 = new Point(c, d);
            double length = lineSegment.Length;
            increment = 1 / (2 * length);
            overlapFractionThreshold = l_threshold;
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
        public double getFractionContainedInBox(RectangleF b, Bitmap mask)
        {
            double eta = 0;
            Size size = Line.P2Offset;
            double currentX = p1.X + eta * size.Width;
            double currentY = p1.Y + eta * size.Height;
            double lastX = -1;
            double lastY = -1;
            int totalPixelCount = 0;
            int overlapCount = 0;

            do
            {
                if ((lastX == currentX) && (lastY == currentY)) continue;

                totalPixelCount++;

                bool isInside = b.Contains((int)currentX, (int)currentY);

                if (mask.GetPixel((int)currentX, (int)currentY).ToArgb() == (~0) )
                {
                    overlapCount++;
                }

                lastX = currentX; lastY = currentY;
                eta += increment;
                currentX = p1.X + eta * size.Width;
                currentY = p1.Y + eta * size.Height;
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
        public double getFractionInForeground(Bitmap mask)
        {
            double eta = 0;
            Size size = Line.P2Offset;
            double currentX = p1.X + eta * size.Width;
            double currentY = p1.Y + eta * size.Height;
            double lastX = -1;
            double lastY = -1;
            int totalPixelCount = 0;
            int overlapCount = 0;

            do
            {
                if ((lastX == currentX) && (lastY == currentY)) continue;

                totalPixelCount++;

                if ( mask.GetPixel( (int)currentX, (int)currentY ).ToString() == "Color [A=255, R=255, G=255, B=255]" )
                {
                    overlapCount++;
                }

                lastX = currentX; lastY = currentY;
                eta += increment;
                currentX = p1.X + eta * size.Width;
                currentY = p1.Y + eta * size.Height;
            } while (eta <= 1);

            double fraction = (double)overlapCount / (double)totalPixelCount;
            return fraction;
        }


        /// <summary>
        /// Finds the box with the maximum overlap fraction with this <see cref="DetectionLine"/>.
        /// </summary>
        /// <param name="boxes">The list of <see cref="Box"/> objects to check, representing the bounding boxes of items in frame.</param>
        /// <param name="mask">A mask detailing the precise layout of items in the frame using black to indicate vacant space, and white to indicate occupied space.</param>
        /// <returns>Returns a <see cref="Tuple{double, Box}"/> containing both the maximum overlap fraction found, and the <see cref="Box"/> associated with that overlap.</returns>
        public (double frac, IFramedItem b) getMaximumFractionContainedInAnyBox(IList<IFramedItem> boxes, Bitmap mask)
        {
            double maxOverlapFraction = 0;
            IFramedItem maxB = null;
            for (int boxNo = 0; boxNo < boxes.Count; boxNo++)
            {
                IFramedItem b = boxes[boxNo];
                StatisticRectangle sr = new StatisticRectangle( from item in b.ItemIDs select item.BoundingBox );
                double area = sr.Mean.Width * sr.Mean.Height;
                if (area < MIN_BOX_SIZE) continue;
                double overlapFraction = getFractionContainedInBox(sr.Mean, mask);
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
        public (bool occupied, IFramedItem box) isOccupied(IList<IFramedItem> boxes, Bitmap mask)
        {
            (double frac, IFramedItem b) = getMaximumFractionContainedInAnyBox(boxes, mask);
            if (frac >= overlapFractionThreshold)
            {
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
        public bool isOccupied(Bitmap mask)
        {
            double frac = getFractionInForeground(mask);
            if (frac >= overlapFractionThreshold)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return p1.X + "\t" + p1.Y + "\t" + p2.X + "\t" + p2.Y + "\t" + overlapFractionThreshold;
        }
    }
}
