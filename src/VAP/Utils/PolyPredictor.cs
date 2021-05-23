using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MathNet.Numerics;
using Utils.ShapeTools;

namespace Utils
{
    public class PolyPredictor : PathPredictor
    {
        public override bool CanPredict( IItemPath path, int frameIndex )
        {
            return path.FrameIndices.Count > 0;
        }

        public override Rectangle Predict( IItemPath path, int frameIndex )
        {
            int count = path.FramedItems.Count;

            if( count >= 2 )
            {
                return PolyPredict( path, frameIndex, Math.Min( 3, count - 1 ) );
            }
            if ( path.FramedItems.Count == 1 )
            {
                return StaticPredict( path, frameIndex );
            }
            throw new InvalidOperationException( "The provided path is empty, so no prediction can be made." );
        }

        private Rectangle PolyPredict( IItemPath path, int frameIndex, int order )
        {
            // Linear Regression for each member of the rectangles.
            var fEnum = from int n in path.FrameIndices
                        select (double)n;
            double[] fVals = fEnum.ToArray();

            var meanRects = from IFramedItem fi in path.FramedItems
                            select MeanBound( fi );

            var xEnum = from RectangleF r in meanRects
                        select (double)r.X;
            var yEnum = from RectangleF r in meanRects
                        select (double)r.Y;
            var widthEnum = from RectangleF r in meanRects
                        select (double)r.Width;
            var heightEnum = from RectangleF r in meanRects
                        select (double)r.Height;

            double[] xCoefs = Fit.Polynomial( fEnum.ToArray(),
                                              xEnum.ToArray(),
                                              order );
            double[] yCoefs = Fit.Polynomial( fEnum.ToArray(),
                                              yEnum.ToArray(),
                                              order );
            double[] widthCoefs = Fit.Polynomial( fEnum.ToArray(),
                                                   widthEnum.ToArray(),
                                                   order );
            double[] heightCoefs = Fit.Polynomial( fEnum.ToArray(),
                                                   heightEnum.ToArray(),
                                                   order );

            RectangleF fPrediction = new RectangleF( (float)Polynomial.Evaluate(frameIndex, xCoefs),
                                                     (float)Polynomial.Evaluate(frameIndex, yCoefs),
                                                     (float)Polynomial.Evaluate(frameIndex, widthCoefs),
                                                     (float)Polynomial.Evaluate(frameIndex, heightCoefs) );

            return RectangleTools.RoundRectF( fPrediction );
        }

        private Rectangle StaticPredict( IItemPath path, int frameIndex )
        {
            int givenIndex = path.FrameIndices[0];
            var ids = path.FramedItems[0].ItemIDs;

            int indexDelta = frameIndex-givenIndex;
            var mean = MeanBound( path.FramedItems[0] );
            return new Rectangle( (int)( mean.X + 0.5f ),
                                  (int)( mean.Y + 0.5f ),
                                  (int)( mean.Width + 0.5f ),
                                  (int)( mean.Height + 0.5f ) );
        }

        private Rectangle CubicPredict( IItemPath path, int frameIndex )
        {
            throw new NotImplementedException();
        }

        private static RectangleF MeanBound( IFramedItem framedItem )
        {
            var rectangles = from IItemID itemId in framedItem.ItemIDs
                             select itemId.BoundingBox;

            var statRect = new StatisticRectangle( rectangles );

            return statRect.Mean;
        }
    }
}
