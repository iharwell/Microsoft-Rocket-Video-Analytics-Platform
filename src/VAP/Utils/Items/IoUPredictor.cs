// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Utils.ShapeTools;

namespace Utils.Items
{
    public class IoUPredictor : PathPredictor
    {
        public override bool CanPredict(IItemPath path, int frameIndex)
        {
            return path.FramedItems.Count > 0;
        }

        public override Rectangle Predict(IItemPath path, int frameIndex)
        {
            int bestIndex;
            int bestIndexAlt;
            int bestFrameAlt = int.MinValue;
            int bestFrame = int.MinValue;
            IFramedItem bestItem = null;
            IFramedItem bestItemAlt = null;

            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                int frameInd = path.FramedItems[i].Frame.FrameIndex;

                if (Math.Abs(frameIndex - frameInd) < Math.Abs(bestFrame - frameIndex))
                {
                    bestIndex = i;
                    bestFrame = frameInd;
                    bestItem = path.FramedItems[i];

                    bestIndexAlt = int.MinValue;
                    bestFrameAlt = int.MinValue;
                    bestItemAlt = null;
                }
                else if (Math.Abs(frameIndex - frameInd) == Math.Abs(bestFrame - frameIndex))
                {
                    bestIndexAlt = i;
                    bestFrameAlt = frameInd;
                    bestItemAlt = path.FramedItems[i];
                }
            }

            if (bestItem == null)
            {
                throw new InvalidOperationException();
            }

            if (bestItemAlt == null)
            {
                var median = bestItem.MeanBounds;
                return new Rectangle((int)(median.X + 0.5), (int)(median.Y + 0.5), (int)(median.Width + 0.5), (int)(median.Height + 0.5));
            }

            else
            {
                var mean = bestItem.MeanBounds;
                var meanAlt = bestItemAlt.MeanBounds;
                return new Rectangle((int)((mean.X + meanAlt.X + 1) / 2),
                                     (int)((mean.Y + meanAlt.Y + 1) / 2),
                                     (int)((mean.Width + meanAlt.Width + 1) / 2),
                                     (int)((mean.Height + meanAlt.Height + 1) / 2));

            }

        }
    }
}
