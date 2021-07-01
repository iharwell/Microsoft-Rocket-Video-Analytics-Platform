// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Utils.ShapeTools;

namespace Utils
{
    /// <summary>
    /// A simple line defined by two points.
    /// </summary>
    public struct LineSegment
    {
        /// <summary>
        /// The first point of the line segment.
        /// </summary>
        public Point P1;
        /// <summary>
        /// The second point of the line segment.
        /// </summary>
        public Point P2;

        /// <summary>
        /// Creates a <see cref="LineSegment"/> using the provided points.
        /// </summary>
        /// <param name="p1">The first point of the line segment.</param>
        /// <param name="p2">The second point of the line segment.</param>
        public LineSegment(Point p1, Point p2)
        {
            P1 = p1;
            P2 = p2;
        }

        /// <summary>
        /// Calculates the midpoint of the line segment.
        /// </summary>
        public Point MidPoint { get { return new Point((P1.X + P2.X) / 2, (P1.Y + P2.Y) / 2); } }

        /// <summary>
        /// Calculates the bounding box of the line segment.
        /// </summary>
        public Rectangle BoundingBox { get { return new Rectangle(Math.Min(P1.X, P2.X), Math.Min(P1.Y, P2.Y), Math.Abs(P2.X - P1.X), Math.Abs(P2.Y - P1.Y)); } }

        /// <summary>
        /// Calculates the length of the line segment.
        /// </summary>
        public double Length
        {
            get
            {
                double dx = P2.X - P1.X;
                double dy = P2.Y - P1.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        /// <summary>
        /// Calculates the offset from P1 to P2.
        /// </summary>
        public Size P2Offset
        {
            get
            {
                return new Size(P2.X - P1.X, P2.Y - P1.Y);
            }
        }
        public (bool, PointF) Intersect(Point p1, Point p2)
        {
            PointF intersection;
            if (p1.X == p2.X)
            {
                (int a1, int b1, int c1) = Formulas.LineEquation(P1, P2);
                intersection = new PointF(p1.X, -(a1 * p1.X - c1) / ((float)b1));
            }
            else if (p1.Y == p2.Y)
            {
                (int a1, int b1, int c1) = Formulas.LineEquation(P1, P2);
                intersection = new PointF(p1.X, -(a1 * p1.X - c1) / ((float)b1));
            }
            else
            {
                (int a1, int b1, int c1) = Formulas.LineEquation(P1, P2);
                (int a2, int b2, int c2) = Formulas.LineEquation(p1, p2);

                float ar = a2 / (float)a1;

                float b3 = b2 - b1 * ar;
                float c3 = c2 - c1 * ar;

                float y = -c3 / b3;
                float x = -(y * b2 + c2) / a2;
                intersection = new PointF(x, y);
            }
            return (BoundingBox.Contains(intersection), intersection);
        }
        public (bool, PointF) Intersect(LineSegment segment)
        {
            PointF intersection;
            if (segment.P1.X == segment.P2.X)
            {
                (int a1, int b1, int c1) = Formulas.LineEquation(P1, P2);
                intersection = new PointF(segment.P1.X, -(a1 * segment.P1.X - c1) / ((float)b1));
            }
            else if (segment.P1.Y == segment.P2.Y)
            {
                (int a1, int b1, int c1) = Formulas.LineEquation(P1, P2);
                intersection = new PointF(segment.P1.X, -(a1 * segment.P1.X - c1) / ((float)b1));
            }
            else
            {
                (int a1, int b1, int c1) = Formulas.LineEquation(P1, P2);
                (int a2, int b2, int c2) = Formulas.LineEquation(segment.P1, segment.P2);

                float ar = a2 / (float)a1;

                float b3 = b2 - b1 * ar;
                float c3 = c2 - c1 * ar;

                float y = -c3 / b3;
                float x = -(y * b2 + c2) / a2;
                intersection = new PointF(x, y);
            }
            return (BoundingBox.Contains(intersection), intersection);
        }


        public PointF ClosestPointTo(PointF targetPoint)
        {
            (int aS, int bS, int cS) = Formulas.LineEquation(P1, P2);
            float a = -aS;
            float b = bS;
            float c = -(targetPoint.X * a + targetPoint.Y * b);

            float ar = a / (float)aS;

            float b3 = b - bS * ar;
            float c3 = c - cS * ar;

            float y = -c3 / b3;
            float x = -(y * b + c) / a;
            return new PointF(x, y);
        }
        public bool IsVertical => P1.X == P2.X && P1.Y != P2.Y;
        public bool IsHorizontal => P1.Y == P2.Y && P1.X != P2.X;
    }
}
