using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.ShapeTools
{
    public class StatisticRectangle
    {
        public StatisticRectangle( IEnumerable<Rectangle> rectangles )
        {
            Rectangle runningSum = new Rectangle(0,0,0,0);
            Rectangle bbox = new Rectangle(0,0,0,0);
            int count = 0;

            bool SizeItemsSet = false;
            Rectangle smallitem = new Rectangle(0,0,0,0);
            Rectangle largeitem = new Rectangle(0,0,0,0);

            foreach ( Rectangle rect in rectangles )
            {
                runningSum.X += rect.X;
                runningSum.Y += rect.Y;
                runningSum.Height += runningSum.Height;
                runningSum.Width += runningSum.Width;

                ++count;

                if ( !SizeItemsSet )
                {
                    SizeItemsSet = true;
                    smallitem = rect;
                    largeitem = rect;
                    bbox = rect;
                }
                else
                {
                    if ( IsSmaller( rect, smallitem ) )
                    {
                        smallitem = rect;
                    }
                    else if ( IsSmaller( largeitem, rect ) )
                    {
                        largeitem = rect;
                    }

                    int xResult = Math.Min( bbox.X, rect.X );
                    int yResult = Math.Min( bbox.Y, rect.Y );
                    int widthResult = Math.Max( bbox.Right, rect.Right ) - xResult;
                    int heightResult = Math.Max( bbox.Bottom, rect.Bottom ) - yResult;

                    bbox = new Rectangle( xResult, yResult, widthResult, heightResult );
                }
            }

            Mean = new RectangleF( 1.0f * runningSum.X / count,
                                   1.0f * runningSum.Y / count,
                                   1.0f * runningSum.Width / count,
                                   1.0f * runningSum.Height / count );
            OverallBoundingBox = bbox;
            SmallestItem = smallitem;
            LargestItem = largeitem;

        }

        public Rectangle OverallBoundingBox { get; }

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
