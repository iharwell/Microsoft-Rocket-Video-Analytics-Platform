using System;
using System.Collections.Generic;
using Utils.Items;
using System.Linq;
using System.Drawing;
using Utils.ShapeTools;

namespace MotionTracker
{
    public class MotionTracker
    {
        public static IItemPath GetPathFromIdAndBuffer( IFramedItem framedID, IList<IList<IFramedItem>> buffer, IPathPredictor predictor, double similarityThreshold )
        {
            IItemPath itemPath = new ItemPath();
            itemPath.FramedItems.Add( framedID );
            int givenIndex = framedID.Frame.FrameIndex;
            // Sort the contents of the buffer by frame index.
            var orgFrames = GroupByFrame(buffer);

            int minFrame = orgFrames[0][0].Frame.FrameIndex;

            int startingIndex = givenIndex - minFrame;
            List<int> counts = new List<int>();

            InductionPass( predictor, ( similarityThreshold + 0.5 ) / 1.5, itemPath, orgFrames, ref startingIndex );
            if ( orgFrames.Count == 0 )
            {
                return itemPath;
            }
            counts.Add( itemPath.FramedItems.Count );

            InductionPass( predictor, ( similarityThreshold + 0.3 ) / 1.3, itemPath, orgFrames, ref startingIndex );


            if ( orgFrames.Count == 0 )
            {
                return itemPath;
            }
            counts.Add( itemPath.FramedItems.Count );

            InductionPass( predictor, ( similarityThreshold + 0.1 ) / 1.1, itemPath, orgFrames, ref startingIndex );

            if ( orgFrames.Count == 0 )
            {
                return itemPath;
            }
            counts.Add( itemPath.FramedItems.Count );

            InductionPass( predictor, similarityThreshold, itemPath, orgFrames, ref startingIndex );
            counts.Add( itemPath.FramedItems.Count );
            /*for ( int i = 0; i < Math.Max( startingIndex, orgFrames.Last().First().Frame.FrameIndex - givenIndex ); ++i )
            {
                if ( ( startingIndex + i ) < orgFrames.Count )
                {

                    TestAndAdd( orgFrames[startingIndex + i], predictor, itemPath, similarityThreshold );
                }
                if ( ( startingIndex - i ) >= 0 && i != 0 )
                {
                    TestAndAdd( orgFrames[startingIndex - i], predictor, itemPath, similarityThreshold );
                }
            }
            for ( int i = framedID.Frame.FrameIndex - minFrame - 1; i >= 0; --i )
            {
                TestAndAdd( orgFrames[i], predictor, itemPath, similarityThreshold );
            }
            for ( int i = framedID.Frame.FrameIndex - minFrame + 1; i < orgFrames.Count; ++i )
            {
                TestAndAdd( orgFrames[i], predictor, itemPath, similarityThreshold );
            }*/

            for ( int i = 0; i < counts.Count; i++ )
            {
                Console.WriteLine( "Pass " + ( i + 1 ) + ": " + counts[i] );
            }
            return itemPath;
        }

        private static void InductionPass( IPathPredictor predictor, double similarityThreshold, IItemPath itemPath, IList<IList<IFramedItem>> orgFrames, ref int startingIndex)
        {
            int lowIndex = startingIndex -1;
            int highIndex = startingIndex;

            while ( lowIndex >= 0 || highIndex < orgFrames.Count )
            {
                int currentSize = itemPath.FramedItems.Count;
                for ( ; lowIndex >= 0; --lowIndex )
                {
                    TestAndAdd( orgFrames[lowIndex], predictor, itemPath, similarityThreshold );
                    if ( currentSize != itemPath.FramedItems.Count )
                    {
                        orgFrames.RemoveAt( lowIndex );
                        --lowIndex;
                        --highIndex;
                        --startingIndex;
                        currentSize = itemPath.FramedItems.Count;
                        break;
                    }
                }
                for ( ; highIndex < orgFrames.Count; ++highIndex )
                {
                    TestAndAdd( orgFrames[highIndex], predictor, itemPath, similarityThreshold );
                    if ( currentSize != itemPath.FramedItems.Count )
                    {
                        orgFrames.RemoveAt( highIndex );
                        break;
                    }
                }
            }
        }

        private static void TestAndAdd( IList<IFramedItem> itemsInFrame, IPathPredictor predictor, IItemPath itemPath, double similarityThreshold )
        {
            if ( itemsInFrame.Count == 0 )
            {
                return;
            }
            int frameIndex = itemsInFrame.First().Frame.FrameIndex;
            Rectangle prediction = predictor.Predict(itemPath, frameIndex);

            double bestSim = 0;
            int closestIndex = -1;
            for ( int j = 0; j < itemsInFrame.Count; j++ )
            {
                var fItem = itemsInFrame[j];
                double sim = fItem.Similarity(prediction);
                if ( sim > bestSim )
                {
                    bestSim = sim;
                    closestIndex = j;
                }
            }

            if ( bestSim > similarityThreshold )
            {
                itemPath.FramedItems.Add( itemsInFrame[closestIndex] );
            }

        }

        private static IList<IList<IFramedItem>> GroupByFrame( IList<IList<IFramedItem>> buffer )
        {
            var allFramedItems = from IList<IFramedItem> subList in buffer
                                 from IFramedItem item in subList
                                 select item;

            int minFrame = buffer[0][0].Frame.FrameIndex;
            int maxFrame = buffer[0][0].Frame.FrameIndex;

            foreach( var item in allFramedItems )
            {
                int frameIndex = item.Frame.FrameIndex;
                minFrame = Math.Min( minFrame, frameIndex );
                maxFrame = Math.Max( maxFrame, frameIndex );
            }

            IList<IList<IFramedItem>> organizedFrames = new List<IList<IFramedItem>>(maxFrame-minFrame);

            for ( int i = minFrame; i <= maxFrame; i++ )
            {
                organizedFrames.Add( new List<IFramedItem>() );
            }

            foreach ( var item in allFramedItems )
            {
                int frameIndex = item.Frame.FrameIndex;
                organizedFrames[frameIndex - minFrame].Add( item );
            }
            return organizedFrames;
        }
    }
}
