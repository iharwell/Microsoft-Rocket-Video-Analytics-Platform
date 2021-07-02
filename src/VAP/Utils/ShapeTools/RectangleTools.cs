// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace Utils.ShapeTools
{

    public static class RectangleTools
    {

        public static Rectangle RoundRectF(this RectangleF rectf)
        {
            return new Rectangle((int)(rectf.X + 0.5),
                                  (int)(rectf.Y + 0.5),
                                  (int)(rectf.Width + 0.5),
                                  (int)(rectf.Height + 0.5));
        }
        public static bool Contains(this Rectangle rect, PointF point)
        {
            return point.X <= rect.Right
                && point.X >= rect.Left
                && point.Y <= rect.Bottom
                && point.Y >= rect.Top;
        }

        public static PointF Center(this Rectangle rect)
        {
            return new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
        }
        public static PointF Center(this RectangleF rect)
        {
            return new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
        }
        public static PointF TopLeft(this RectangleF rect)
        {
            return rect.Location;
        }
        public static Point TopLeft(this Rectangle rect)
        {
            return rect.Location;
        }
        public static PointF BottomLeft(this RectangleF rect)
        {
            return new PointF(rect.X, rect.Y + rect.Height);
        }
        public static Point BottomLeft(this Rectangle rect)
        {
            return new Point(rect.X, rect.Y + rect.Height);
        }
        public static PointF TopRight(this RectangleF rect)
        {
            return new PointF(rect.X + rect.Width, rect.Y);
        }
        public static Point TopRight(this Rectangle rect)
        {
            return new Point(rect.X + rect.Width, rect.Y);
        }
        public static PointF BottomRight(this RectangleF rect)
        {
            return new PointF(rect.X + rect.Width, rect.Y + rect.Height);
        }
        public static Point BottomRight(this Rectangle rect)
        {
            return new Point(rect.X + rect.Width, rect.Y + rect.Height);
        }

        public static double DiagonalSquared(this Rectangle rect)
        {
            return PointTools.DistanceSquared(rect.Location, new Point(rect.Right, rect.Bottom));
        }

        public static double DiagonalSquared(this RectangleF rect)
        {
            return PointTools.DistanceSquared(rect.Location, new PointF(rect.Right, rect.Bottom));
        }
        public static Rectangle ScaleFromCenter(this Rectangle rect, double scaleFactor)
        {
            Size s = rect.Size;
            SizeF newSize = new SizeF((float)(s.Width * scaleFactor), (float)(s.Height * scaleFactor));

            return (new RectangleF(rect.X - (newSize.Width - s.Width) / 2, rect.Y - (newSize.Height - s.Height) / 2, newSize.Width, newSize.Height)).RoundRectF();
        }
        public static RectangleF ScaleFromCenter(this RectangleF rect, double scaleFactor)
        {
            SizeF s = rect.Size;
            SizeF newSize = new SizeF((float)(s.Width * scaleFactor), (float)(s.Height * scaleFactor));

            return (new RectangleF(rect.X - (newSize.Width - s.Width) / 2, rect.Y - (newSize.Height - s.Height) / 2, newSize.Width, newSize.Height));
        }
        public static float IntersectionOverUnion(this Rectangle rect, Rectangle rect2)
        {
            Rectangle intersect;
            {
                Point intersectLoc = new Point(Math.Max(rect.Left, rect2.Left), Math.Max(rect.Top, rect2.Top));
                intersect = new Rectangle(intersectLoc.X, intersectLoc.Y, Math.Min(rect.Right, rect2.Right) - intersectLoc.X, Math.Min(rect.Bottom, rect2.Bottom) - intersectLoc.Y);
            }
            if (intersect.Width < 0 || intersect.Height < 0)
            {
                return 0.0f;
            }
            long intArea = intersect.Width * (long)intersect.Height;
            long r1Area = rect.Width * (long)rect.Height;
            long r2Area = rect2.Width * (long)rect2.Height;

            long unionArea = r1Area + r2Area - intArea;

            return intArea / (1.0f * unionArea);
        }
        public static float IntersectionOverUnion(this RectangleF rect, Rectangle rect2)
        {
            RectangleF intersect;
            {
                PointF intersectLoc = new PointF(Math.Max(rect.Left, rect2.Left), Math.Max(rect.Top, rect2.Top));
                intersect = new RectangleF(intersectLoc.X, intersectLoc.Y, Math.Min(rect.Right, rect2.Right) - intersectLoc.X, Math.Min(rect.Bottom, rect2.Bottom) - intersectLoc.Y);
            }
            if (intersect.Width < 0 || intersect.Height < 0)
            {
                return 0.0f;
            }
            float intersectArea = intersect.Width * intersect.Height;
            float r1Area = rect.Width * rect.Height;
            float r2Area = rect2.Width * (long)rect2.Height;

            float unionArea = r1Area + r2Area - intersectArea;

            return intersectArea / unionArea;
        }
        public static float IntersectionOverUnion(this Rectangle rect, RectangleF rect2)
        {
            RectangleF intersect;
            {
                PointF intersectLoc = new PointF(Math.Max(rect.Left, rect2.Left), Math.Max(rect.Top, rect2.Top));
                intersect = new RectangleF(intersectLoc.X, intersectLoc.Y, Math.Min(rect.Right, rect2.Right) - intersectLoc.X, Math.Min(rect.Bottom, rect2.Bottom) - intersectLoc.Y);
            }
            if (intersect.Width < 0 || intersect.Height < 0)
            {
                return 0.0f;
            }
            float intersectArea = intersect.Width * intersect.Height;
            float r1Area = rect.Width * (long)rect.Height;
            float r2Area = rect2.Width * rect2.Height;

            float unionArea = r1Area + r2Area - intersectArea;

            return intersectArea / unionArea;
        }
        public static float IntersectionOverUnion(this RectangleF rect, RectangleF rect2)
        {
            RectangleF intersect;
            {
                PointF intersectLoc = new PointF(Math.Max(rect.Left, rect2.Left), Math.Max(rect.Top, rect2.Top));
                intersect = new RectangleF(intersectLoc.X, intersectLoc.Y, Math.Min(rect.Right, rect2.Right) - intersectLoc.X, Math.Min(rect.Bottom, rect2.Bottom) - intersectLoc.Y);
            }
            if (intersect.Width < 0 || intersect.Height < 0)
            {
                return 0.0f;
            }
            float intersectArea = intersect.Width * intersect.Height;
            float r1Area = rect.Width * rect.Height;
            float r2Area = rect2.Width * rect2.Height;

            float unionArea = r1Area + r2Area - intersectArea;

            return intersectArea / unionArea;
        }

        public static Point GetNearestPointToLine(this Rectangle boundingBox, LineSegment segment)
        {
            Point leftSegPoint;
            Point rightSegPoint;
            Point topSegPoint;
            Point bottomSegPoint;

            int segYMax;
            int segYMin;
            int segXMax;
            int segXMin;

            Rectangle sBox = segment.BoundingBox;
            if (segment.P1.X < segment.P2.X)
            {
                leftSegPoint = segment.P1;
                rightSegPoint = segment.P2;
                segXMin = segment.P1.X;
                segXMax = segment.P2.X;
            }
            else
            {
                leftSegPoint = segment.P2;
                rightSegPoint = segment.P1;
                segXMin = segment.P2.X;
                segXMax = segment.P1.X;
            }

            if (segment.P1.Y < segment.P2.Y)
            {
                topSegPoint = segment.P1;
                bottomSegPoint = segment.P2;
                segYMin = segment.P1.Y;
                segYMax = segment.P2.Y;
            }
            else
            {
                topSegPoint = segment.P2;
                bottomSegPoint = segment.P1;
                segYMin = segment.P2.Y;
                segYMax = segment.P1.Y;
            }

            // LR Situations
            // x- x+ L R
            // x- L x+ R
            // x- L R x+
            // L x- x+ R
            // L x- R x+
            // L R x- x+

            // TB Situations
            // y- y+ T B
            // y- T y+ B
            // y- T B y+
            // T y- y+ B
            // T y- B y+
            // T B y- y+


            if (sBox.Left > boundingBox.Right)
            {
                // LR Situations
                // L R x- x+
                if (sBox.Top > boundingBox.Bottom)
                {
                    return boundingBox.BottomRight();
                }
                else if (sBox.Bottom < boundingBox.Top)
                {
                    return boundingBox.TopRight();
                }
                else
                {
                    // Some vertical overlap, but line is on the right.
                    return new Point(boundingBox.Right, Math.Min(boundingBox.Bottom, Math.Max(boundingBox.Top, leftSegPoint.Y)));
                }
            }
            else if (sBox.Right < boundingBox.Left)
            {
                // LR Situations
                // x- x+ L R
                if (sBox.Top > boundingBox.Bottom)
                {
                    return boundingBox.BottomLeft();
                }
                else if (sBox.Bottom < boundingBox.Top)
                {
                    return boundingBox.TopLeft();
                }
                else
                {
                    // Some vertical overlap, but line is on the left.
                    return new Point(boundingBox.Left, Math.Min(boundingBox.Bottom, Math.Max(boundingBox.Top, rightSegPoint.Y)));
                }
            }
            else
            {
                if (sBox.Top > boundingBox.Bottom)
                {
                    // Some horizontal overlap, but line is below box.
                    return new Point(Math.Min(boundingBox.Right, Math.Max(boundingBox.Left, topSegPoint.X)), boundingBox.Bottom);
                }
                else if (sBox.Bottom < boundingBox.Top)
                {
                    // Some horizontal overlap, but line is above box.
                    return new Point(Math.Min(boundingBox.Right, Math.Max(boundingBox.Left, topSegPoint.X)), boundingBox.Top);
                }
            }

            // LR Situations
            // x- L x+ R
            // x- L R x+
            // L x- x+ R
            // L x- R x+

            // TB Situations
            // y- T y+ B
            // y- T B y+
            // T y- y+ B
            // T y- B y+
            PointF center = boundingBox.Center();

            if (segment.IsVertical)
            {
                if (boundingBox.Contains(bottomSegPoint))
                {
                    if (boundingBox.Contains(topSegPoint))
                    {
                        return new Point(segXMin, Math.Min(segYMax, Math.Max(segYMin, (int)(center.Y + 0.5f))));
                    }
                    if (center.Y > segYMax)
                    {
                        return bottomSegPoint;
                    }
                    return new Point(segXMin, (int)(center.Y + 0.5f));
                }
                if (boundingBox.Contains(topSegPoint))
                {
                    if (center.Y < segYMin)
                    {
                        return topSegPoint;
                    }
                    return new Point(segXMin, (int)(center.Y + 0.5f));
                }
                if (segYMin < boundingBox.Top && segYMax > boundingBox.Bottom)
                {
                    return new Point(segXMin, (int)(center.Y + 0.5f));
                }
            }
            if (segment.IsHorizontal)
            {
                if (boundingBox.Contains(rightSegPoint))
                {
                    if (boundingBox.Contains(leftSegPoint))
                    {
                        return new Point(Math.Min(segXMax, Math.Max(segXMin, (int)(center.X + 0.5f))), segYMin);
                    }
                    if (center.X > segXMax)
                    {
                        return rightSegPoint;
                    }
                    return new Point((int)(center.X + 0.5f), segYMin);
                }
                if (boundingBox.Contains(leftSegPoint))
                {
                    if (center.X < segXMin)
                    {
                        return leftSegPoint;
                    }
                    return new Point((int)(center.X + 0.5f), segYMin);
                }
                if (segXMin < boundingBox.Left && segXMax > boundingBox.Right)
                {
                    return new Point((int)(center.X + 0.5f), segYMin);
                }
            }


            bool containsP1 = boundingBox.Contains(segment.P1);
            bool containsP2 = boundingBox.Contains(segment.P2);
            PointF closest = segment.ClosestPointTo(center);
            // Single intersect and fully contained segment.
            if (containsP1 || containsP2)
            {
                if (sBox.Contains(closest))
                {
                    return new Point((int)(closest.X + 0.5f), (int)(closest.Y + 0.5f));
                }
                else
                {
                    if (containsP1 && !containsP2)
                    {
                        return segment.P1;
                    }
                    else if (!containsP1 && containsP2)
                    {
                        return segment.P2;
                    }
                    else // containsP1 && containsP2
                    {
                        if (closest.X > segXMax)
                        {
                            return rightSegPoint;
                        }
                        else // (closest.X < segXMin)
                        {
                            return leftSegPoint;
                        }
                    }
                }
            }

            // Easy and special cases handled.

            (bool lHitA, var lInt) = segment.Intersect(boundingBox.TopLeft(), boundingBox.BottomLeft());
            (bool rHitA, var rInt) = segment.Intersect(boundingBox.TopRight(), boundingBox.BottomRight());
            (bool tHitA, var tInt) = segment.Intersect(boundingBox.TopLeft(), boundingBox.BottomLeft());
            (bool bHitA, var bInt) = segment.Intersect(boundingBox.BottomLeft(), boundingBox.BottomRight());

            bool lHit = lHitA && lInt.Y > boundingBox.Top && lInt.Y < boundingBox.Bottom;
            bool rHit = rHitA && rInt.Y > boundingBox.Top && rInt.Y < boundingBox.Bottom;
            bool tHit = tHitA && tInt.X > boundingBox.Left && tInt.X < boundingBox.Right;
            bool bHit = bHitA && bInt.X > boundingBox.Left && bInt.X < boundingBox.Right;

            int hitCount = 0;
            if (lHit) { hitCount++; }
            if (rHit) { hitCount++; }
            if (tHit) { hitCount++; }
            if (bHit) { hitCount++; }

            (LRPosition xPos, TBPosition yPos) = DeterminePositionSituation(boundingBox, segment);

            if (hitCount == 0)
            {
                if (xPos == LRPosition.SegmentLeftPart)
                {
                    // LR Situations
                    // x- L x+ R

                    // TB Situations
                    // y- T y+ B
                    // y- T B y+
                    // T y- y+ B
                    // T y- B y+
                    if (yPos == TBPosition.SegmentAbovePart)
                    {
                        // TB Situations
                        // y- T y+ B
                        return boundingBox.TopLeft();
                    }
                    else if (yPos == TBPosition.SegmentBelowPart)
                    {
                        // TB Situations
                        // T y- B y+
                        return boundingBox.BottomLeft();
                    }
                    else // yPos == TBPosition.SegmentNeither
                    {
                        // TB Situations
                        // y- T B y+

                        // Note: the T y- y+ B form cannot exist here because it would involve a single intersection
                        // in order to meet the partial left requirement.
                        if (leftSegPoint.Y > rightSegPoint.Y)
                        {
                            return boundingBox.BottomLeft();
                        }
                        else
                        {
                            return boundingBox.TopLeft();
                        }
                    }
                }

                if (xPos == LRPosition.SegmentRightPart)
                {
                    // LR Situations
                    // L x- R x+

                    // TB Situations
                    // y- T y+ B
                    // y- T B y+
                    // T y- y+ B
                    // T y- B y+
                    if (yPos == TBPosition.SegmentAbovePart)
                    {
                        return boundingBox.TopRight();
                    }
                    else if (yPos == TBPosition.SegmentBelowPart)
                    {
                        return boundingBox.BottomLeft();
                    }
                    else // yPos == TBPosition.SegmentNeither
                    {
                        if (leftSegPoint.Y > rightSegPoint.Y)
                        {
                            return boundingBox.BottomLeft();
                        }
                        else
                        {
                            return boundingBox.TopLeft();
                        }
                    }
                }


                // LR Situations
                // x- L R x+
                // L x- x+ R

                // TB Situations
                // y- T y+ B
                // y- T B y+
                // T y- y+ B
                // T y- B y+

                if (yPos == TBPosition.SegmentAbovePart)
                {
                    if (topSegPoint.X < bottomSegPoint.X)
                    {
                        return boundingBox.TopRight();
                    }
                    else
                    {
                        return boundingBox.TopLeft();
                    }
                }
                if (yPos == TBPosition.SegmentBelowPart)
                {
                    if (topSegPoint.X < bottomSegPoint.X)
                    {
                        return boundingBox.BottomLeft();
                    }
                    else
                    {
                        return boundingBox.BottomRight();
                    }
                }
            }

            // 2 intersections

            Debug.Assert(hitCount == 2);

            Point closestInt = new Point((int)(closest.X + 0.5f), (int)(closest.Y + 0.5f));
            if (lHit)
            {
                if (rHit)
                {
                    // Straight through, so closest point is easy.
                    return closestInt;
                }
                bool contains = boundingBox.Contains(closest);
                if (tHit)
                {
                    if (contains)
                    {
                        return closestInt;
                    }
                    else if (Math.Abs(closest.X - tInt.X) < Math.Abs(closest.X - lInt.X))
                    {
                        return new Point((int)(tInt.X + 0.5f), (int)(tInt.Y + 0.5f));
                    }
                    else
                    {
                        return new Point((int)(lInt.X + 0.5f), (int)(lInt.Y + 0.5f));
                    }
                }
                if (bHit)
                {
                    if (contains)
                    {
                        return closestInt;
                    }
                    else if (Math.Abs(closest.X - bInt.X) < Math.Abs(closest.X - lInt.X))
                    {
                        return new Point((int)(bInt.X + 0.5f), (int)(bInt.Y + 0.5f));
                    }
                    else
                    {
                        return new Point((int)(lInt.X + 0.5f), (int)(lInt.Y + 0.5f));
                    }
                }
            }
            if (rHit)
            {
                bool contains = boundingBox.Contains(closest);
                if (tHit)
                {
                    if (contains)
                    {
                        return closestInt;
                    }
                    else if (Math.Abs(closest.X - tInt.X) < Math.Abs(closest.X - rInt.X))
                    {
                        return new Point((int)(tInt.X + 0.5f), (int)(tInt.Y + 0.5f));
                    }
                    else
                    {
                        return new Point((int)(rInt.X + 0.5f), (int)(rInt.Y + 0.5f));
                    }
                }
                if (bHit)
                {
                    if (contains)
                    {
                        return closestInt;
                    }
                    else if (Math.Abs(closest.X - bInt.X) < Math.Abs(closest.X - rInt.X))
                    {
                        return new Point((int)(bInt.X + 0.5f), (int)(bInt.Y + 0.5f));
                    }
                    else
                    {
                        return new Point((int)(rInt.X + 0.5f), (int)(rInt.Y + 0.5f));
                    }
                }
            }
            Debug.Assert(tHit);
            Debug.Assert(bHit);
            return closestInt;
        }


        public static (LRPosition xPos, TBPosition yPos) DeterminePositionSituation(this Rectangle rect, LineSegment segment)
        {
            int segYMax = Math.Max(segment.P1.Y, segment.P2.Y);
            int segYMin = Math.Min(segment.P1.Y, segment.P2.Y);
            int segXMax = Math.Max(segment.P1.X, segment.P2.X);
            int segXMin = Math.Min(segment.P1.X, segment.P2.X);

            return DeterminePositionSituation(segYMax, segYMin, segXMax, segXMin, rect);
        }

        private static (LRPosition xPos, TBPosition yPos) DeterminePositionSituation(int segYMax, int segYMin, int segXMax, int segXMin, Rectangle rect)
        {
            LRPosition lrPosition;

            if (segXMax < rect.Left)
            {
                // x- x+ L R
                lrPosition = LRPosition.SegmentLeftFull;
            }
            else if (rect.Right < segXMin)
            {
                // L R x- x+
                lrPosition = LRPosition.SegmentRightFull;
            }
            else if (segXMin < rect.Left)
            {
                // x- L ? ?
                if (segXMax < rect.Right)
                {
                    // x- L x+ R
                    lrPosition = LRPosition.SegmentLeftPart;
                }
                else
                {
                    // x- L R x+
                    lrPosition = LRPosition.SegmentStraddle;
                }
            }
            else
            {
                // L x- ? ?
                if (segXMax < rect.Right)
                {
                    // L x- x+ R
                    lrPosition = LRPosition.SegmentInner;
                }
                else
                {
                    // L x- R x+
                    lrPosition = LRPosition.SegmentRightPart;
                }
            }

            TBPosition tbPosition;

            if (segYMax < rect.Left)
            {
                // y- y+ T B
                tbPosition = TBPosition.SegmentAboveFull;
            }
            else if (rect.Bottom < segYMin)
            {
                // T B y- y+
                tbPosition = TBPosition.SegmentBelowFull;
            }
            else if (segYMin < rect.Top)
            {
                // y- T ? ?
                if (segYMax < rect.Bottom)
                {
                    // y- T y+ B
                    tbPosition = TBPosition.SegmentAbovePart;
                }
                else
                {
                    // y- T B y+
                    tbPosition = TBPosition.SegmentStraddle;
                }
            }
            else
            {
                // T y- ? ?
                if (segYMax < rect.Bottom)
                {
                    // T y- y+ B
                    tbPosition = TBPosition.SegmentInner;
                }
                else
                {
                    // T y- B y+
                    tbPosition = TBPosition.SegmentBelowPart;
                }
            }

            return (lrPosition, tbPosition);
        }
    }
}
