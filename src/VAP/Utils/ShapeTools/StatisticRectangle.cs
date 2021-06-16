// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MathNet.Numerics.Statistics;
using Utils.Items;

namespace Utils.ShapeTools
{
    public class StatisticRectangle
    {
        public StatisticRectangle(IEnumerable<IItemID> items)
        {
            Rectangle runningSum = new Rectangle(0, 0, 0, 0);
            Rectangle bbox = new Rectangle(0, 0, 0, 0);
            int count = 0;

            bool sizeItemsSet = false;
            Rectangle smallitem = new Rectangle(0, 0, 0, 0);
            Rectangle largeitem = new Rectangle(0, 0, 0, 0);

            foreach (IItemID item in items)
            {
                Rectangle rect = item.BoundingBox;
                runningSum.X += rect.X;
                runningSum.Y += rect.Y;
                runningSum.Height += rect.Height;
                runningSum.Width += rect.Width;

                ++count;

                if (!sizeItemsSet)
                {
                    sizeItemsSet = true;
                    smallitem = rect;
                    largeitem = rect;
                    bbox = rect;
                }
                else
                {
                    if (IsSmaller(rect, smallitem))
                    {
                        smallitem = rect;
                    }
                    else if (IsSmaller(largeitem, rect))
                    {
                        largeitem = rect;
                    }

                    int xResult = Math.Min(bbox.X, rect.X);
                    int yResult = Math.Min(bbox.Y, rect.Y);
                    int widthResult = Math.Max(bbox.Right, rect.Right) - xResult;
                    int heightResult = Math.Max(bbox.Bottom, rect.Bottom) - yResult;

                    bbox = new Rectangle(xResult, yResult, widthResult, heightResult);
                }
            }

            var xSet = from item in items
                       select (double)item.BoundingBox.X;
            var ySet = from item in items
                       select (double)item.BoundingBox.Y;
            var wSet = from item in items
                       select (double)item.BoundingBox.Width;
            var hSet = from item in items
                       select (double)item.BoundingBox.Height;

            Median = new RectangleF((float)xSet.Median(), (float)ySet.Median(), (float)wSet.Median(), (float)hSet.Median());

            Mean = new RectangleF(1.0f * runningSum.X / count,
                                   1.0f * runningSum.Y / count,
                                   1.0f * runningSum.Width / count,
                                   1.0f * runningSum.Height / count);
            OverallBoundingBox = bbox;
            SmallestItem = smallitem;
            LargestItem = largeitem;

        }

        public static RectangleF MedianBox(IEnumerable<Rectangle> rectangles)
        {
            var xSet = from r in rectangles
                       select (double)r.X;
            var ySet = from r in rectangles
                       select (double)r.Y;
            var wSet = from r in rectangles
                       select (double)r.Width;
            var hSet = from r in rectangles
                       select (double)r.Height;

            return new RectangleF((float)xSet.Median(), (float)ySet.Median(), (float)wSet.Median(), (float)hSet.Median());
        }
        public static RectangleF MeanBox(IEnumerable<IItemID> ids)
        {
            var xSet = from id in ids
                       select (double)id.BoundingBox.X;
            var ySet = from id in ids
                       select (double)id.BoundingBox.Y;
            var wSet = from id in ids
                       select (double)id.BoundingBox.Width;
            var hSet = from id in ids
                       select (double)id.BoundingBox.Height;

            return new RectangleF((float)xSet.Mean(), (float)ySet.Mean(), (float)wSet.Mean(), (float)hSet.Mean());
        }
        public static RectangleF MeanBox(IList<IItemID> ids)
        {
            float sumx = 0;
            float sumy = 0;
            float sumw = 0;
            float sumh = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                Rectangle r = ids[i].BoundingBox;

                sumx += r.X;
                sumy += r.Y;
                sumw += r.Width;
                sumh += r.Height;
            }
            int c = ids.Count;

            return new RectangleF((float)sumx/c, (float)sumy/c, (float)sumw/c, (float)sumh/c);
        }
        public static RectangleF UpdateMeanBox(IList<IItemID> ids, RectangleF oldMean, int oldCount)
        {
            float sumx = oldMean.X*oldCount;
            float sumy = oldMean.Y*oldCount;
            float sumw = oldMean.Width*oldCount;
            float sumh = oldMean.Height * oldCount;
            for (int i = oldCount; i < ids.Count; i++)
            {
                Rectangle r = ids[i].BoundingBox;

                sumx += r.X;
                sumy += r.Y;
                sumw += r.Width;
                sumh += r.Height;
            }
            int c = ids.Count;

            return new RectangleF((float)sumx / c, (float)sumy / c, (float)sumw / c, (float)sumh / c);
        }

        public StatisticRectangle(IEnumerable<Rectangle> rectangles)
        {
            Rectangle runningSum = new Rectangle(0, 0, 0, 0);
            Rectangle bbox = new Rectangle(0, 0, 0, 0);
            int count = 0;

            bool sizeItemsSet = false;
            Rectangle smallitem = new Rectangle(0, 0, 0, 0);
            Rectangle largeitem = new Rectangle(0, 0, 0, 0);

            foreach (Rectangle rect in rectangles)
            {
                runningSum.X += rect.X;
                runningSum.Y += rect.Y;
                runningSum.Height += rect.Height;
                runningSum.Width += rect.Width;

                ++count;

                if (!sizeItemsSet)
                {
                    sizeItemsSet = true;
                    smallitem = rect;
                    largeitem = rect;
                    bbox = rect;
                }
                else
                {
                    if (IsSmaller(rect, smallitem))
                    {
                        smallitem = rect;
                    }
                    else if (IsSmaller(largeitem, rect))
                    {
                        largeitem = rect;
                    }

                    int xResult = Math.Min(bbox.X, rect.X);
                    int yResult = Math.Min(bbox.Y, rect.Y);
                    int widthResult = Math.Max(bbox.Right, rect.Right) - xResult;
                    int heightResult = Math.Max(bbox.Bottom, rect.Bottom) - yResult;

                    bbox = new Rectangle(xResult, yResult, widthResult, heightResult);
                }
            }

            var xSet = from r in rectangles
                       select (double)r.X;
            var ySet = from r in rectangles
                       select (double)r.Y;
            var wSet = from r in rectangles
                       select (double)r.Width;
            var hSet = from r in rectangles
                       select (double)r.Height;

            Median = new RectangleF((float)xSet.Median(), (float)ySet.Median(), (float)wSet.Median(), (float)hSet.Median());

            Mean = new RectangleF(1.0f * runningSum.X / count,
                                   1.0f * runningSum.Y / count,
                                   1.0f * runningSum.Width / count,
                                   1.0f * runningSum.Height / count);
            OverallBoundingBox = bbox;
            SmallestItem = smallitem;
            LargestItem = largeitem;

        }

        public Rectangle OverallBoundingBox { get; }

        public RectangleF Median { get; }
        public Rectangle SmallestItem { get; }
        public Rectangle LargestItem { get; }
        public RectangleF Mean { get; }

        private static bool IsSmaller(Rectangle a, Rectangle b)
        {
            return a.Width * a.Height < b.Width * b.Height;
        }

        /*private static bool IsSmaller( Rectangle a, Rectangle b )
        {
            return a.Width * a.Height < b.Width * b.Height;
        }*/
    }
}
