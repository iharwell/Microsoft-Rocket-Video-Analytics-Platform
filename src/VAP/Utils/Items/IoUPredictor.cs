using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Utils.ShapeTools;

namespace Utils.Items
{
    public class IoUPredictor : PathPredictor
    {
        public override bool CanPredict( IItemPath path, int frameIndex )
        {
            return path.FramedItems.Count > 0;
        }

        public override Rectangle Predict( IItemPath path, int frameIndex )
        {
            int bestIndex = -1;
            int bestFrame = -1;
            IFramedItem bestItem = null;

            for ( int i = 0; i < path.FramedItems.Count; i++ )
            {
                int frameInd = path.FramedItems[i].Frame.FrameIndex;

                if( Math.Abs(frameIndex - frameInd ) < Math.Abs( bestFrame - frameIndex) )
                {
                    bestIndex = i;
                    bestFrame = frameInd;
                    bestItem = path.FramedItems[i];
                }
            }

            if ( bestItem == null )
            {
                throw new InvalidOperationException();
            }

            StatisticRectangle sr = new StatisticRectangle(bestItem.ItemIDs);
            var median = sr.Median;
            return new Rectangle( (int)( median.X + 0.5 ), (int)( median.Y + 0.5 ), (int)( median.Width + 0.5 ), (int)( median.Height + 0.5 ) );
        }
    }
}
