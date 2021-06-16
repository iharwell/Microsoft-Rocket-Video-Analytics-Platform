// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
        public static IItemPath GetPathFromIdAndBuffer(IFramedItem framedID, IList<IList<IFramedItem>> buffer, IPathPredictor predictor, double similarityThreshold)
        {
            IItemPath itemPath = new ItemPath();
            itemPath.FramedItems.Add(framedID);
            if (buffer.Count == 0)
            {
                return itemPath;
            }
            int givenIndex = framedID.Frame.FrameIndex;
            // Sort the contents of the buffer by frame index.
            // var orgFrames = GroupByFrame(buffer);
            var orgFrames = buffer;

            int minFrame = orgFrames[0][0].Frame.FrameIndex;
            int maxFrame = orgFrames[^1][0].Frame.FrameIndex;

            int startingIndex = givenIndex - minFrame;
            startingIndex = Math.Max(minFrame, startingIndex) - minFrame;
            startingIndex = Math.Min(maxFrame - minFrame, startingIndex);
            List<int> counts = new List<int>();

            InductionPass(predictor, (similarityThreshold + 0.5) / 1.5, itemPath, orgFrames, ref startingIndex);
            if (orgFrames.Count == 0)
            {
                return itemPath;
            }
            counts.Add(itemPath.FramedItems.Count);

            InductionPass(predictor, (similarityThreshold + 0.3) / 1.3, itemPath, orgFrames, ref startingIndex);


            if (orgFrames.Count == 0)
            {
                return itemPath;
            }
            counts.Add(itemPath.FramedItems.Count);

            InductionPass(predictor, (similarityThreshold + 0.1) / 1.1, itemPath, orgFrames, ref startingIndex);

            if (orgFrames.Count == 0)
            {
                return itemPath;
            }
            counts.Add(itemPath.FramedItems.Count);

            InductionPass(predictor, similarityThreshold, itemPath, orgFrames, ref startingIndex);
            counts.Add(itemPath.FramedItems.Count);
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

            for (int i = 0; i < counts.Count; i++)
            {
                Console.WriteLine("Pass " + (i + 1) + ": " + counts[i]);
            }
            return itemPath;
        }

        public static IItemPath ExpandPathFromBuffer(IItemPath itemPath, IList<IList<IFramedItem>> buffer, IPathPredictor predictor, double similarityThreshold)
        {
            if (buffer.Count == 0)
            {
                return itemPath;
            }

            int givenIndex = itemPath.FramedItems.First().Frame.FrameIndex;

            // Sort the contents of the buffer by frame index.
            // var orgFrames = GroupByFrame(buffer);
            var orgFrames = buffer;
            int minFrame = orgFrames[0][0].Frame.FrameIndex;

            int startingIndex = givenIndex - minFrame;
            List<int> counts = new List<int>();

            RemoveUsedFrames(itemPath, orgFrames, ref startingIndex);
            if (orgFrames.Count == 0)
            {
                return itemPath;
            }

            //InductTriggeredFrame( itemPath, orgFrames );

            InductionPass(predictor, (similarityThreshold + 0.5) / 1.5, itemPath, orgFrames, ref startingIndex);
            if (orgFrames.Count == 0)
            {
                return itemPath;
            }
            counts.Add(itemPath.FramedItems.Count);

            InductionPass(predictor, (similarityThreshold + 0.3) / 1.3, itemPath, orgFrames, ref startingIndex);


            if (orgFrames.Count == 0)
            {
                return itemPath;
            }
            counts.Add(itemPath.FramedItems.Count);

            InductionPass(predictor, (similarityThreshold + 0.1) / 1.1, itemPath, orgFrames, ref startingIndex);

            if (orgFrames.Count == 0)
            {
                return itemPath;
            }
            counts.Add(itemPath.FramedItems.Count);

            InductionPass(predictor, similarityThreshold, itemPath, orgFrames, ref startingIndex);

            counts.Add(itemPath.FramedItems.Count);

            for (int i = 0; i < counts.Count; i++)
            {
                Console.WriteLine("Pass " + (i + 1) + ": " + counts[i]);
            }
            return itemPath;
        }

        private static void RemoveUsedFrames(IItemPath itemPath, IList<IList<IFramedItem>> orgFrames, ref int startingIndex)
        {
            var usedFrames = from framedItem in itemPath.FramedItems
                             let frame = framedItem.Frame.FrameIndex
                             orderby frame
                             select frame;

            List<int> usedIndices = new List<int>(usedFrames);

            int startingFrameNumber = orgFrames[0][0].Frame.FrameIndex + startingIndex;

            /*int i = 0;
            foreach ( int used in usedFrames )
            {
                while ( i < orgFrames.Count )
                {
                    if ( orgFrames[i].Count == 0 ) // remove an empty frame
                    {
                        orgFrames.RemoveAt( i );
                        if ( i <= startingIndex )
                        {
                            --startingIndex;
                        }
                        continue;
                    }

                    int frameIndex = orgFrames[i].First().Frame.FrameIndex;


                    if ( frameIndex == used ) // remove an already present frame
                    {
                        orgFrames.RemoveAt( i );
                        if ( i <= startingIndex )
                        {
                            --startingIndex;
                        }
                        break;
                    }

                    // unused but populated frame
                    ++i;
                }
            }*/
            int i = 0;
            while (i < orgFrames.Count)
            {/*
                if (orgFrames[i].Count == 0)
                {
                    // Remove empty
                    if (i <= startingIndex && startingIndex > 0)
                    {
                        --startingIndex;
                    }
                    orgFrames.RemoveAt(i);
                    --i;
                    continue;
                }
                if (usedFrames.Contains(orgFrames[i].First().Frame.FrameIndex))
                {
                    if (i <= startingIndex && startingIndex > 0)
                    {
                        --startingIndex;
                    }
                    orgFrames.RemoveAt(i);
                    --i;
                }*/
                if (orgFrames[i].Count == 0)
                {
                    // Remove empty
                    if (i <= startingIndex && startingIndex > 0)
                    {
                        --startingIndex;
                    }
                    orgFrames.RemoveAt(i);
                    continue; // New item is now present at index i
                }
                int orgFramesFrameNumber = orgFrames[i][0].Frame.FrameIndex;
                if (usedIndices.BinarySearch(orgFramesFrameNumber) >= 0)
                {
                    // This index is already used.
                    if (i <= startingIndex && startingIndex > 0)
                    {
                        --startingIndex;
                    }
                    orgFrames.RemoveAt(i);
                    continue; // New item is now present at index i
                }
                ++i;
            }

            // Verify that the resulting startingIndex is the closest option to the initial starting frame number available.

            int bestStart = startingIndex;
            int bestStartDistance = 99999;

            for (int j = 0; j < orgFrames.Count; j++)
            {
                int frameNumber = orgFrames[j][0].Frame.FrameIndex;
                int distance = Math.Abs(startingFrameNumber - frameNumber);
                if (distance < bestStartDistance)
                {
                    bestStart = j;
                    bestStartDistance = distance;
                }
                if (distance == 0)
                {
                    break;
                }
            }
            startingIndex = bestStart;
        }

        private static void InductTriggeredFrame(IItemPath itemPath, IList<IList<IFramedItem>> orgFrames)
        {
            IItemID triggeredItemID = null;

            var lastFrame = orgFrames.Last();
            for (int i = lastFrame.Count - 1; i >= 0; --i)
            {
                var framedItem = lastFrame[i];
                for (int j = framedItem.ItemIDs.Count - 1; j >= 0; --j)
                {
                    var itemID = framedItem.ItemIDs[j];
                    if (itemID.FurtherAnalysisTriggered)
                    {
                        triggeredItemID = itemID;
                        break;
                    }
                }
                if (triggeredItemID != null)
                {
                    break;
                }
            }
            /**/
            if (triggeredItemID != null)
            {
                var secondLastFrame = orgFrames[orgFrames.Count - 1];
                IFramedItem closestItem = null;
                double maxSim = -999999999;
                for (int i = 0; i < secondLastFrame.Count; i++)
                {
                    var framedItem = secondLastFrame[i];
                    double sim = framedItem.Similarity(triggeredItemID.BoundingBox);
                    if (sim > maxSim)
                    {
                        maxSim = sim;
                        closestItem = framedItem;
                    }
                }

                if (closestItem != null)
                {
                    itemPath.FramedItems.Add(closestItem);
                    orgFrames.RemoveAt(orgFrames.Count - 2);
                }
            }
        }

        public static void SealPath(IItemPath path, IList<IList<IFramedItem>> frameBuffer)
        {
            //var orgFrames = GroupByFrame(frameBuffer);
            var orgFrames = frameBuffer;
            int x = 0;
            RemoveUsedFrames(path, orgFrames, ref x);
            int minFrame = int.MaxValue;
            int maxFrame = int.MinValue;
            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                int frameNum = path.FrameIndex(i);
                minFrame = Math.Min(minFrame, frameNum);
                maxFrame = Math.Max(maxFrame, frameNum);
            }

            for (int i = 0; i < orgFrames.Count; i++)
            {
                var frameGroup = orgFrames[i];
                for (int j = 0; j < frameGroup.Count; j++)
                {
                    IFrame f = frameGroup[j].Frame;
                    int fgNum = frameGroup[j].Frame.FrameIndex;
                    if (fgNum > minFrame && fgNum < maxFrame)
                    {
                        path.FramedItems.Add(new FramedItem(f, new FillerID()));
                        break;
                    }
                }
            }
        }
        private static void InductionPass(IPathPredictor predictor, double similarityThreshold, IItemPath itemPath, IList<IList<IFramedItem>> orgFrames, ref int startingIndex)
        {
            int lowIndex = startingIndex - 1;
            int highIndex = startingIndex;

            while (lowIndex >= 0 || highIndex < orgFrames.Count)
            {
                int currentSize = itemPath.FramedItems.Count;
                for (; lowIndex >= 0; --lowIndex)
                {
                    TestAndAdd(orgFrames[lowIndex], predictor, itemPath, similarityThreshold);
                    if (currentSize != itemPath.FramedItems.Count || orgFrames[lowIndex].Count == 0)
                    {
                        orgFrames.RemoveAt(lowIndex);
                        --lowIndex;
                        --highIndex;
                        --startingIndex;
                        currentSize = itemPath.FramedItems.Count;
                        break;
                    }
                }
                for (; highIndex < orgFrames.Count; ++highIndex)
                {
                    TestAndAdd(orgFrames[highIndex], predictor, itemPath, similarityThreshold);
                    if (currentSize != itemPath.FramedItems.Count || orgFrames[highIndex].Count == 0)
                    {
                        orgFrames.RemoveAt(highIndex);
                        break;
                    }
                }
            }
        }

        public static bool TestAndAdd(IList<IFramedItem> itemsInFrame, IPathPredictor predictor, IItemPath itemPath, double similarityThreshold)
        {
            if (itemsInFrame.Count == 0)
            {
                return false;
            }

            int frameIndex = itemsInFrame.First().Frame.FrameIndex;
            Rectangle prediction = predictor.Predict(itemPath, frameIndex);

            double bestSim = 0;
            int closestIndex = -1;
            for (int j = 0; j < itemsInFrame.Count; j++)
            {
                var fItem = itemsInFrame[j];
                double sim = fItem.Similarity(prediction);
                if (sim > bestSim)
                {
                    bestSim = sim;
                    closestIndex = j;
                }
            }

            if (bestSim > similarityThreshold)
            {
                itemPath.FramedItems.Add(itemsInFrame[closestIndex]);
                return true;
            }
            return false;
        }

        public static IList<IList<IFramedItem>> InsertIntoSortedBuffer( IList<IList<IFramedItem>> buffer, IList<IFramedItem> unsortedSet )
        {
            if(unsortedSet.Count == 0)
            {
                return buffer;
            }

            if(buffer.Count == 0)
            {
                buffer.Add(unsortedSet);
                return GroupByFrame(buffer);
            }

            int bmin = buffer[0][0].Frame.FrameIndex;
            int bmax = buffer[^1][0].Frame.FrameIndex;
            int min = bmin;
            int max = bmax;


            for (int i = 0; i < unsortedSet.Count; i++)
            {
                int index = unsortedSet[i].Frame.FrameIndex;
                min = Math.Min(min, index);
                max = Math.Max(max, index);
            }

            IList<IList<IFramedItem>> destList;
            if (bmin == min)
            {
                destList = buffer;
            }
            else
            {
                destList = new List<IList<IFramedItem>>(max-min+1);
            }
            for (int i = min+destList.Count; i <= max; i++)
            {
                destList.Add(new List<IFramedItem>());
            }

            HashSet<int> shortcutFrames = new HashSet<int>();
            foreach ( var item in unsortedSet )
            {
                int tgtIndex = item.Frame.FrameIndex - min;
                IList<IFramedItem> dst = destList[tgtIndex];
                if (item.Frame.FrameIndex > bmax)
                {
                    dst.Add(item);
                    continue;
                }

                if(dst.Count == 0)
                {
                    shortcutFrames.Add(tgtIndex);
                }
                else if (dst.Count == 1 && dst[0] is FillerID)
                {
                    dst.RemoveAt(0);
                    shortcutFrames.Add(tgtIndex);
                }
                if(shortcutFrames.Contains(tgtIndex))
                {
                    dst.Add(item);
                }
                else
                {
                    FramedItem.MergeIntoFramedItemList(item, ref dst);
                }
            }
            return destList;
        }

        public static IList<IList<IFramedItem>> GroupByFrame(IList<IList<IFramedItem>> buffer)
        {
            var allFramedItems = from IList<IFramedItem> subList in buffer
                                 from IFramedItem item in subList
                                 group item by item.Frame.FrameIndex;
                                 //select item;

            int minFrame = buffer[0][0].Frame.FrameIndex;
            int maxFrame = buffer[0][0].Frame.FrameIndex;

            foreach (var grouping in allFramedItems)
            {
                int frameIndex = grouping.Key;
                minFrame = Math.Min(minFrame, frameIndex);
                maxFrame = Math.Max(maxFrame, frameIndex);
            }

            IList<IList<IFramedItem>> organizedFrames = new List<IList<IFramedItem>>(maxFrame - minFrame + 1);

            for (int i = minFrame; i <= maxFrame; i++)
            {
                organizedFrames.Add(new List<IFramedItem>());
            }

            foreach (var grouping in allFramedItems)
            {
                int frameIndex = grouping.Key;
                IList<IFramedItem> itemSet = organizedFrames[frameIndex - minFrame];
                foreach (var item in grouping)
                {
                    FramedItem.MergeIntoFramedItemList(item, ref itemSet);
                }
            }
            return organizedFrames;
        }

        public static void TryMergePaths(ref IList<IItemPath> paths)
        {
            IList<IItemPath> outPaths = new List<IItemPath>();

            Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries = new Dictionary<IItemPath, (int minFrame, int maxFrame)>();

            foreach ( var path in paths)
            {
                boundaries.Add(path, path.GetPathBounds());
            }
            bool outOfRange;
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                outOfRange = false;
                int minFrame;
                int maxFrame;
                (minFrame, maxFrame) = boundaries[path];

                for (int j = i+1; j < paths.Count; j++)
                {
                    var tgtPath = paths[j];
                    int tgtMin;
                    int tgtMax;

                    (tgtMin, tgtMax) = boundaries[path];

                    if( tgtMax - maxFrame > 250 )
                    {
                        outOfRange = true;
                        break;
                    }

                    for (int k = Math.Max(tgtMin,minFrame); k < Math.Min(tgtMax,maxFrame); k++)
                    {
                        var tgtFi = GetFramedItemByFrameNumber(tgtPath, k);
                        if(tgtFi == null)
                        {
                            continue;
                        }
                        var srcFi = GetFramedItemByFrameNumber(path, k);
                        if(srcFi == null)
                        {
                            continue;
                        }
                        if( AreFramedItemsMatched(srcFi, tgtFi))
                        {
                            foreach (var fi in tgtPath.FramedItems)
                            {
                                var dest = GetFramedItemByFrameNumber(path, fi.Frame.FrameIndex);
                                if (dest == null)
                                {
                                    path.FramedItems.Add(fi);
                                }
                                else
                                {
                                    foreach (var id in fi.ItemIDs)
                                    {
                                        if (dest.ItemIDs.Contains(id))
                                        {
                                            continue;
                                        }
                                        else if (HasSimilarID(id, dest.ItemIDs))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            dest.ItemIDs.Add(id);
                                        }
                                    }
                                }
                            }
                            maxFrame = Math.Max(maxFrame, tgtMax);
                            minFrame = Math.Min(minFrame, tgtMin);
                            ++i;
                        }
                    }

                }
                outPaths.Add(path);
            }

            paths = outPaths;
        }

        private static bool HasSimilarID( IItemID id, IList<IItemID> idlist)
        {
            if( id is FillerID )
            {
                return true;
            }
            if(idlist.Count == 1 && idlist[0] is FillerID)
            {
                return false;
            }
            for (int i = 0; i < idlist.Count; i++)
            {
                if( id.BoundingBox == idlist[i].BoundingBox)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool AreFramedItemsMatched( IFramedItem item1, IFramedItem item2)
        {
            if( item1.Frame.FrameIndex != item2.Frame.FrameIndex)
            {
                return false;
            }
            for (int i = 0; i < item1.ItemIDs.Count; i++)
            {
                var id1 = item1.ItemIDs[i];
                for (int j = 0; j < item2.ItemIDs.Count; j++)
                {
                    var id2 = item2.ItemIDs[j];

                    if(id1.Confidence == id2.Confidence && id1.Confidence>0)
                    {
                        return true;
                    }
                    if(id1.BoundingBox.Location == id2.BoundingBox.Location && id1.BoundingBox.Size == id2.BoundingBox.Size)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static IFramedItem GetFramedItemByFrameNumber( IItemPath path, int frameNumber )
        {
            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                var fi = path.FramedItems[i];
                if ( fi.Frame.FrameIndex == frameNumber )
                {
                    return fi;
                }
            }
            return null;
        }
    }
}
