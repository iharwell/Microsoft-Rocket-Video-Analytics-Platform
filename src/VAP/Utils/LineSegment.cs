using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

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
    }
}
