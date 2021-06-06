using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.ShapeTools
{
    public static class PointTools
    {
        public static double DistanceSquared(Point p1, Point p2)
        {
            int dx = p1.X - p2.X;
            int dy = p1.Y - p2.Y;
            return dx * dx + dy * dy;
        }
        public static double DistanceSquared(PointF p1, PointF p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return dx * dx + dy * dy;
        }
    }
}
