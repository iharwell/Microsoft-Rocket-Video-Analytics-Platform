using System;
using System.Collections.Generic;
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

        public static PointF Center(this Rectangle rect)
        {
            return new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
        }
        public static PointF Center(this RectangleF rect)
        {
            return new PointF(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
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
            SizeF NewSize = new SizeF((float)(s.Width * scaleFactor), (float)(s.Height * scaleFactor));

            return (new RectangleF(rect.X - (NewSize.Width - s.Width) / 2, rect.Y - (NewSize.Height - s.Height) / 2, NewSize.Width, NewSize.Height)).RoundRectF();
        }
        public static RectangleF ScaleFromCenter(this RectangleF rect, double scaleFactor)
        {
            SizeF s = rect.Size;
            SizeF NewSize = new SizeF((float)(s.Width * scaleFactor), (float)(s.Height * scaleFactor));

            return (new RectangleF(rect.X - (NewSize.Width - s.Width) / 2, rect.Y - (NewSize.Height - s.Height) / 2, NewSize.Width, NewSize.Height));
        }
    }
}
