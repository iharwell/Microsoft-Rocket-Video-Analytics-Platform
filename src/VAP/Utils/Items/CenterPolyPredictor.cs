// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace Utils.Items
{
    public class CenterPolyPredictor : PathPredictor
    {

        public CenterPolyPredictor()
        {
            CachedPath = null;
            CachedPathFilteredSize = -1;
            CachedModel = null;
        }

        public override bool CanPredict(IItemPath path, int frameIndex)
        {
            return path.FramedItems.Count > 0;
        }

        public override Rectangle Predict(IItemPath path, int frameIndex)
        {
            var model = GetModel(path);

            return ShapeTools.RectangleTools.RoundRectF(model.Predict(frameIndex));
        }

        private CenterPolyPathModel GetModel(IItemPath path)
        {
            var centersAndSizes = from item in path.FramedItems
                                  where !(item.ItemIDs.Count == 1 && item.ItemIDs[0] is FillerID)
                                  let mb = item.MeanBounds
                                  select (new PointF(mb.X + mb.Width / 2, mb.Y + mb.Height / 2), mb.Size);
            List<(PointF center, SizeF size)> cAndS = new List<(PointF center, SizeF size)>(centersAndSizes);
            if (object.ReferenceEquals(path, CachedPath) && CachedPathFilteredSize == cAndS.Count)
            {
                return CachedModel;
            }
            else
            {
                var model = MakeModel(path);
                CachedPath = path;
                CachedPathFilteredSize = cAndS.Count;
                CachedModel = model;
                return model;
            }
        }

        private IItemPath CachedPath;
        private int CachedPathFilteredSize;
        private CenterPolyPathModel CachedModel;

        private static CenterPolyPathModel MakeModel(IItemPath path)
        {

            var centersAndSizes = from item in path.FramedItems
                                  where !(item.ItemIDs.Count == 1 && item.ItemIDs[0] is FillerID)
                                  let mb = item.MeanBounds
                                  select (item.Frame.FrameIndex, new PointF(mb.X + mb.Width / 2, mb.Y + mb.Height / 2), mb.Size);

            List<(int frame, PointF center, SizeF size)> cAndS = new List<(int frame, PointF center, SizeF size)>(centersAndSizes);
            int order = Math.Min(3, Math.Max(0, cAndS.Count / 4));
            var model = new CenterPolyPathModel();

            var frameSet = (from entry in cAndS
                            select (double)entry.frame).ToArray();
            var centerXSet = (from entry in cAndS
                              select (double)entry.center.X).ToArray();
            var centerYSet = (from entry in cAndS
                              select (double)entry.center.Y).ToArray();
            var wSet = (from entry in cAndS
                        select (double)entry.size.Width).ToArray();
            var hSet = (from entry in cAndS
                        select (double)entry.size.Height).ToArray();

            model.CenterXCoefs = MathNet.Numerics.Fit.Polynomial(frameSet, centerXSet, order);
            model.CenterYCoefs = MathNet.Numerics.Fit.Polynomial(frameSet, centerYSet, order);
            model.WidthCoefs = MathNet.Numerics.Fit.Polynomial(frameSet, wSet, order);
            model.HeightCoefs = MathNet.Numerics.Fit.Polynomial(frameSet, hSet, order);

            return model;
        }
    }
}
