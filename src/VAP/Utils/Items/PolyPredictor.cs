// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MathNet.Numerics;
using Utils.ShapeTools;

namespace Utils.Items
{
    /// <summary>
    ///   An <see cref="IPathPredictor" /> that uses a polynomial regression on the for corners of
    ///   the known bounding boxes to predict the bounding box location in another frame.
    /// </summary>
    public class PolyPredictor : PathPredictor
    {

        /// <inheritdoc/>
        public override bool CanPredict(IItemPath path, int frameIndex)
        {
            return path.FramedItems.Count > 0;
        }

        /// <inheritdoc/>
        public override Rectangle Predict(IItemPath path, int frameIndex)
        {
            int count = path.FramedItems.Count;
            int order = Math.Min(3, Math.Max(0, count / 4));
            return PolyPredict(path, frameIndex, order);/*
            if( count >= 2 )
            {
                return PolyPredict( path, frameIndex, Math.Min( 3, count - 1 ) );
            }
            if ( path.FramedItems.Count == 1 )
            {
                return StaticPredict( path, frameIndex );
            }*/
            throw new InvalidOperationException("The provided path is empty, so no prediction can be made.");
        }

        /// <inheritdoc/>
        private Rectangle PolyPredict(IItemPath path, int frameIndex, int order)
        {
            // Linear Regression for each member of the rectangles.
            var fEnum = from item in path.FramedItems
                        select (double)item.Frame.FrameIndex;
            double[] fVals = fEnum.ToArray();

            var meanRects = from IFramedItem fi in path.FramedItems
                            select MedianBound(fi);

            var xEnum = from RectangleF r in meanRects
                        select (double)r.X;
            var yEnum = from RectangleF r in meanRects
                        select (double)r.Y;
            var widthEnum = from RectangleF r in meanRects
                            select (double)r.Width;
            var heightEnum = from RectangleF r in meanRects
                             select (double)r.Height;

            double[] xCoefs = Fit.Polynomial(fEnum.ToArray(),
                                              xEnum.ToArray(),
                                              order);
            double[] yCoefs = Fit.Polynomial(fEnum.ToArray(),
                                              yEnum.ToArray(),
                                              order);
            double[] widthCoefs = Fit.Polynomial(fEnum.ToArray(),
                                                   widthEnum.ToArray(),
                                                   order);
            double[] heightCoefs = Fit.Polynomial(fEnum.ToArray(),
                                                   heightEnum.ToArray(),
                                                   order);

            RectangleF fPrediction = new RectangleF((float)Polynomial.Evaluate(frameIndex, xCoefs),
                                                     (float)Polynomial.Evaluate(frameIndex, yCoefs),
                                                     (float)Polynomial.Evaluate(frameIndex, widthCoefs),
                                                     (float)Polynomial.Evaluate(frameIndex, heightCoefs));

            return RectangleTools.RoundRectF(fPrediction.ScaleFromCenter(1 + 0.1 / Math.Max(1, order)));
        }

        private static RectangleF MedianBound(IFramedItem framedItem)
        {
            var rectangles = from IItemID itemId in framedItem.ItemIDs
                             select itemId.BoundingBox;

            var statRect = new StatisticRectangle(rectangles);

            return statRect.Median;
        }
    }
}
