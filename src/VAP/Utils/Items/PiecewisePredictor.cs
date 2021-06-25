// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.Items
{
    public class PiecewisePredictor : PathPredictor
    {
        /// <summary>
        ///   The number of items needed per order for the model. 
        ///   Increasing this reduces noise, but also results in a simpler motion model.
        /// </summary>
        public double OverSampling { get; set; }

        public PiecewisePredictor()
            : this(4.0)
        { }

        public PiecewisePredictor(double overSampling)
        {
            OverSampling = overSampling;
        }

        public override bool CanPredict(IItemPath path, int frameIndex)
        {
            return path.FramedItems.Count > 0;
        }

        public override Rectangle Predict(IItemPath path, int frameIndex)
        {
            // TODO(iharwell): Add distance-based order for extrapolated values.

            return ShapeTools.RectangleTools.RoundRectF(Interpolate(path, frameIndex));
        }

        private RectangleF Interpolate(IItemPath path, int frameIndex)
        {
            var nearest = path.NearestItems(frameIndex);
            int closestDistance = nearest.First().Frame.FrameIndex - frameIndex;
            int itemsRequired = (int)(((OrderForFrameOffset(closestDistance) + 1) * OverSampling) + 0.5);
            var enumerator = nearest.GetEnumerator();
            List<IFramedItem> itemList = new List<IFramedItem>(itemsRequired);
            for (int i = 0; i < itemsRequired; i++)
            {
                if (!enumerator.MoveNext())
                {
                    break;
                }
                itemList.Add(enumerator.Current);
            }

            double[] frameIndices = new double[itemList.Count];

            double[] xSet = new double[itemList.Count];

            double[] ySet = new double[itemList.Count];

            double[] wSet = new double[itemList.Count];

            double[] hSet = new double[itemList.Count];

            for (int i = 0; i < itemList.Count; i++)
            {
                RectangleF rect = itemList[i].MeanBounds;
                frameIndices[i] = itemList[i].Frame.FrameIndex;
                xSet[i] = rect.X + rect.Width / 2;
                ySet[i] = rect.Y + rect.Width / 2;
                wSet[i] = rect.Width;
                hSet[i] = rect.Height;
            }

            int actualOrder = (int)((itemList.Count / OverSampling) + 0.5);

            CenterPolyPathModel model = new CenterPolyPathModel();
            model.CenterXCoefs = MathNet.Numerics.Fit.Polynomial(frameIndices, xSet, actualOrder);
            model.CenterYCoefs = MathNet.Numerics.Fit.Polynomial(frameIndices, ySet, actualOrder);
            model.WidthCoefs = MathNet.Numerics.Fit.Polynomial(frameIndices, wSet, actualOrder);
            model.HeightCoefs= MathNet.Numerics.Fit.Polynomial(frameIndices, hSet, actualOrder);

            return model.Predict(frameIndex);
        }

        public int OrderForFrameOffset(int offset)
        {
            if (offset <= 3)
            {
                return 1;
            }

            return (int)(Math.Log2(offset));
        }

        public bool IsInterpolated(IItemPath path, int frameIndex)
        {
            (int min, int max ) = path.GetPathBounds();
            return (frameIndex > min) && (frameIndex < max);
        }
    }
}
