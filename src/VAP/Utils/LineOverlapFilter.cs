// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Utils.ShapeTools;

namespace Utils
{
    public class LineOverlapFilter
    {
        public LineOverlapFilter(ISet<LineSegment> lines)
        {}

        public static IDictionary<T, float> GetItemOverlap<T>(IEnumerable<T> items, Func<T, RectangleF> toRectangle, LineSegment segment)
        {
            IDictionary<T, float> overlaps = new Dictionary<T, float>();
            foreach (var item in items)
            {
                overlaps.Add(item, GetOverlapRatio(segment, toRectangle(item)));
            }
            return overlaps;
        }

        private static PointF? GetIntersection(PointF line1p1, PointF line1p2, PointF line2p1, PointF line2p2)
        {
            float x;
            float y;

            float m1 = (line1p2.Y - line1p1.Y) / (line1p2.X - line1p1.X);
            float b1 = line1p1.Y - line1p1.X * m1;

            float m2;
            float b2;

            if (line1p1.X == line1p2.X)
            {
                Debug.Assert(line1p1.Y != line1p2.Y);
                if (line2p1.X == line2p2.X && line2p1.X != line1p1.X)
                {
                    return null;
                }

                // line 1 is vertical, so X must be line1p1.X
                x = line1p1.X;
            }
            else if (line2p1.X == line2p2.X)
            {
                Debug.Assert(line2p1.Y != line2p2.Y);
                x = line2p1.X;
            }
            else
            {
                m2 = (line2p2.Y - line2p1.Y) / (line2p2.X - line2p1.X);
                b2 = line2p1.Y - line2p1.X * m2;

                x = (b2 - b1) / (m1 - m2);
            }

            if (line1p1.Y == line1p2.Y)
            {
                Debug.Assert(line1p1.X != line1p2.X);

                if (line2p1.Y == line2p2.Y && line2p1.Y != line1p1.Y)
                {
                    return null;
                }
                y = line1p1.Y;
            }
            else if (line2p1.Y == line2p2.Y)
            {
                y = line2p1.Y;
            }
            else
            {
                y = m1 * x + b1;
            }

            return new PointF(x, y);
        }

        public static float GetOverlapRatio(LineSegment segment, RectangleF rect)
        {
            if( rect.Contains(segment.P1) && rect.Contains(segment.P2))
            {
                return 1;
            }

            PointF? topIntersect = GetIntersection(segment.P1, segment.P2, rect.TopLeft(), rect.TopRight());
            PointF? bottomIntersect = GetIntersection(segment.P1, segment.P2, rect.BottomLeft(), rect.BottomRight());
            PointF? leftIntersect = GetIntersection(segment.P1, segment.P2, rect.TopLeft(), rect.BottomLeft());
            PointF? rightIntersect = GetIntersection(segment.P1, segment.P2, rect.TopRight(), rect.BottomRight());

            PointF? intersect1;
            PointF? intersect2;

            bool topHit = topIntersect != null && IsOnLine(topIntersect.Value, rect.TopRight(), rect.TopLeft()) && IsOnLine(topIntersect.Value, segment.P1, segment.P2);
            bool bottomHit = bottomIntersect != null && IsOnLine(bottomIntersect.Value, rect.BottomLeft(), rect.BottomRight()) && IsOnLine(bottomIntersect.Value, segment.P1, segment.P2);
            bool leftHit = leftIntersect != null && IsOnLine(leftIntersect.Value, rect.TopLeft(), rect.BottomLeft()) && IsOnLine(leftIntersect.Value, segment.P1, segment.P2);
            bool rightHit = rightIntersect != null && IsOnLine(rightIntersect.Value, rect.TopRight(), rect.BottomRight()) && IsOnLine(rightIntersect.Value, segment.P1, segment.P2);

            if (topHit)
            {
                intersect1 = topIntersect.Value;

                if (bottomHit)
                {
                    intersect2 = bottomIntersect.Value;
                }
                else if (leftHit)
                {
                    intersect2 = leftIntersect.Value;
                }
                else if (rightHit)
                {
                    intersect2 = rightIntersect.Value;
                }
                else
                {
                    intersect2 = null;
                }
            }
            else if (bottomHit)
            {
                // No top intersect.
                intersect1 = bottomIntersect.Value;
                if (leftHit)
                {
                    intersect2 = leftIntersect.Value;
                }
                else if (rightHit)
                {
                    intersect2 = rightIntersect.Value;
                }
                else
                {
                    intersect2 = null;
                }
            }
            else if (leftHit)
            {
                // No top or bottom intersect.
                intersect1 = leftIntersect.Value;
                if (rightHit)
                {
                    intersect2 = rightIntersect.Value;
                }
                else
                {
                    intersect2 = null;
                }
            }
            else if (rightHit)
            {
                // No top, bottom, or left intersect.
                intersect1 = rightIntersect.Value;
                intersect2 = null;
            }
            else
            {
                return 0.0f;
            }

            if (intersect2 == null)
            {
                if (IsInBox(segment.P1, rect))
                {
                    intersect2 = segment.P1;
                }
                else if (IsInBox(segment.P2, rect))
                {
                    intersect2 = segment.P2;
                }
                else
                {
                    Rectangle r = new Rectangle((int)(rect.X + 0.5f), (int)(rect.Y + 0.5f), (int)(rect.Width + 0.5f), (int)(rect.Height + 0.5f));

                    Utils.CheckLineBboxOverlapRatio(segment, r);
                }
            }

            return (Math.Max(intersect1.Value.X, intersect2.Value.X) - Math.Min(intersect1.Value.X, intersect2.Value.X)) / (Math.Max(segment.P1.X, segment.P2.X) - Math.Min(segment.P1.X, segment.P2.X));
        }
        private static bool IsInBox(PointF pointOfInterest, RectangleF rect)
        {
            return (pointOfInterest.X >= rect.Left && pointOfInterest.X <= rect.Right && pointOfInterest.Y <= rect.Bottom && pointOfInterest.Y >= rect.Top);
        }

        private static bool IsOnLine(PointF pointOfInterest, PointF lineP1, PointF lineP2)
        {
            float minX = Math.Min(lineP1.X, lineP2.X);
            float maxX = Math.Max(lineP1.X, lineP2.X);
            float minY = Math.Min(lineP1.Y, lineP2.Y);
            float maxY = Math.Max(lineP1.Y, lineP2.Y);
            if (lineP1.X == lineP2.X && pointOfInterest.Y <= maxY && pointOfInterest.Y >= minY)
            {
                return Math.Abs(pointOfInterest.X - lineP1.X) < 0.000001f;
            }
            if (lineP1.Y == lineP2.Y && pointOfInterest.X <= maxX && pointOfInterest.X >= minX)
            {
                return Math.Abs(pointOfInterest.Y - lineP1.Y) < 0.000001f;
            }

            if (pointOfInterest.X > maxX || pointOfInterest.X < minX || pointOfInterest.Y > maxY || pointOfInterest.Y < minY)
            {
                return false;
            }

            float rx = (pointOfInterest.X - lineP1.X) / (lineP2.X - lineP1.X);


            float ry = (pointOfInterest.Y - lineP1.Y) / (lineP2.Y - lineP1.Y);

            return (rx - ry) < 0.00001f;
        }
        private static bool IsOnLine(PointF pointOfInterest, Point lineP1, Point lineP2)
        {
            return IsOnLine(pointOfInterest, new PointF(lineP1.X, lineP1.Y), new PointF(lineP2.X, lineP2.Y));
        }

        private static bool AreColinear(Point l1p1, Point l1p2, PointF l2p1, PointF l2p2)
        {
            return AreColinear(new PointF(l1p1.X, l1p1.Y), new PointF(l1p2.X, l1p2.Y), l2p1, l2p2);
        }
        private static bool AreColinear(PointF l1p1, PointF l1p2, PointF l2p1, PointF l2p2)
        {
            if (l1p1.X == l1p2.X)
            {
                return l2p1.X == l2p2.X && l2p1.X == l1p1.X;
            }
            if (l1p1.Y == l1p2.Y)
            {
                return l2p1.Y == l2p2.Y && l2p1.Y == l1p1.Y;
            }

            if (l2p1.X == l2p2.X || l2p1.Y == l2p2.Y)
            {
                return false;
            }

            float m1 = (l1p1.Y - l1p2.Y) / (l1p1.X - l1p2.X);
            float m2 = (l2p1.Y - l2p2.Y) / (l2p1.X - l2p2.X);

            if (Math.Abs(m1 - m2) > 0.00001f)
            {
                return false;
            }

            float b1 = l1p1.Y - l1p1.X * m1;
            float b2 = l2p1.Y - l2p1.X * m2;

            return Math.Abs(b1 - b2) < 0.00001f;

        }
    }
}
