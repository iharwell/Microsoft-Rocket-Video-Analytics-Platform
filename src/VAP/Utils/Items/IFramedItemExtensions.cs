using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Utils.ShapeTools;

namespace Utils.Items
{
    public static class IFramedItemExtensions
    {
        public static double Similarity( this IFramedItem item, Rectangle rect )
        {
            StatisticRectangle sr = new StatisticRectangle(item.ItemIDs);
            if ( sr.Median.X <= rect.Right && rect.X <= sr.Median.Right && sr.Median.Y <= rect.Bottom && rect.Y <= sr.Median.Bottom )
            {
                // There is some overlap, so we will give a positive similarity score.
                double ovX = Math.Max( rect.X, sr.Median.X );
                double ovY = Math.Max( rect.Y, sr.Median.Y );
                double ovW = Math.Min( rect.Right, sr.Median.Right ) - ovX;
                double ovH = Math.Min( rect.Bottom, sr.Median.Bottom ) - ovY;
                RectangleF overlap = new RectangleF( (float)ovX, (float)ovY, (float)ovW, (float)ovH );

                double overlapArea = ovW*ovH;
                double srArea = sr.Median.Width*sr.Median.Height;
                double rectArea = rect.Width*rect.Height;
                /*
                double overlapPercentRect = overlapArea/(rect.Width*rect.Height);
                double overlapPercentMean = overlapArea/(sr.Median.Width *sr.Median.Height);
                return overlapPercentRect * overlapPercentMean;*/
                return ( overlapArea ) / ( srArea + rectArea - overlapArea );
            }
            else
            {
                // there is no overlap, so we'll give it a negative similarity score.
                double distSq = PointTools.DistanceSquared(rect.Center(), sr.Median.Center());
                double diagSq1 = rect.DiagonalSquared();
                double diagSq2 = sr.Median.DiagonalSquared();
                double normalizedDistance1 = distSq / diagSq1;
                double normalizedDistance2 = distSq / diagSq2;
                double sizeFactor = Math.Max(sr.Median.Width, rect.Width) / Math.Min(sr.Median.Width, rect.Width)
                                  * Math.Max(sr.Median.Height, rect.Height) / Math.Min(sr.Median.Height, rect.Height);

                return -( normalizedDistance1 * normalizedDistance2 ) * sizeFactor;
            }
        }
    }
}
