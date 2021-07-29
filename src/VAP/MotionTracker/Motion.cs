// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Utils.Items;
using Utils.ShapeTools;

namespace MotionTracker
{
    public static class Motion
    {

        public static float PathVelocity(IList<IFramedItem> framedItems)
        {
            return PathVelocity(framedItems, 0, framedItems.Count - 1);
        }

        public static float PathVelocity(IItemPath path)
        {
            return PathVelocity(path.FramedItems, 0, path.FramedItems.Count - 1);
        }

        public static float PathVelocity(IItemPath path, int startIndex, int endIndex)
        {
            return PathVelocity(path.FramedItems, startIndex, endIndex);
        }

        /*public static float PathVelocity(IList<IFramedItem> framedItems, int startIndex, int endIndex)
        {
            var startItem = framedItems[startIndex];
            var startCenter = startItem.MeanBounds.Center();
            var distFunc = new Func<IFramedItem, float>((IFramedItem item) =>
            {
                PointF p1 = item.MeanBounds.Center();
                PointF p2 = startCenter;

                float dx = p1.X - p2.X;
                float dy = p1.Y - p2.Y;
                return MathF.Sqrt((dx * dx) + (dy * dy));
            });
            var indexFunc = new Func<IFramedItem, float>((IFramedItem item) =>
            {
                float f1 = item.Frame.FrameIndex;
                float f2 = startItem.Frame.FrameIndex;

                return f1 - f2;
            });

            double[] dists = new double[endIndex - startIndex + 1];
            double[] indices = new double[dists.Length];

            for (int i = 0; i+startIndex < framedItems.Count && i+startIndex <= endIndex; i++)
            {
                var item = framedItems[i+startIndex];
                dists[i] = distFunc(item);
                indices[i] = indexFunc(item);
            }

            var result = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(indices, dists);
            return (float)result.Item2;
        }*/
        public static float PathVelocity(IList<IFramedItem> framedItems, int startIndex, int endIndex)
        {
            var coefs = GetCenterLineFunc(framedItems);
            float xsq = coefs.a.X * coefs.a.X;
            float ysq = coefs.a.Y * coefs.a.Y;
            return MathF.Sqrt(xsq + ysq);
        }

        public static (float iou, IFramedItem match) FindBestMatch(IFramedItem testItem, IList<IFramedItem> searchSet)
        {
            return FindBestMatch(testItem, searchSet, null);
        }

        public static (float iou, IFramedItem match) FindBestMatch(IFramedItem testItem, IList<IFramedItem> searchSet, IDictionary<IFramedItem,bool> filterTable)
        {
            return FindBestMatch(testItem.MeanBounds.RoundRectF(), searchSet, filterTable);
        }
        public static (float iou, IFramedItem match) FindBestMatch(Rectangle testRect, IList<IFramedItem> searchSet, IDictionary<IFramedItem, bool> filterTable)
        {
            float bestiou = -1.0f;
            IFramedItem bestMatch = null;

            //Rectangle testRect = RectangleTools.RoundRectF(testItem.MeanBounds);

            for (int i = 0; i < searchSet.Count; i++)
            {
                if (filterTable != null && filterTable.ContainsKey(searchSet[i]) && !filterTable[searchSet[i]])
                {
                    continue;
                }
                Rectangle targetRect = RectangleTools.RoundRectF(searchSet[i].MeanBounds);
                float iou = testRect.IntersectionOverUnion(targetRect);
                if (iou > bestiou)
                {
                    bestiou = iou;
                    bestMatch = searchSet[i];
                }
            }

            return (bestiou, bestMatch);
        }

        public static (Vector2 a, Vector2 b, Vector2 c) GetCenterQuadratic(this IItemPath path)
        {
            int n = path.FramedItems.Count;
            var startPoint = new PointF(0, 0);
            var midPoint = new PointF(0, 0);
            var endPoint = new PointF(0, 0);

            float startFrame = 0;
            float midFrame = 0;
            float endFrame = 0;

            int avgSize = Math.Min(3, n / 3);

            (startPoint, midPoint, endPoint) = GetSmoothedPoints(path, avgSize);
            (startFrame, midFrame, endFrame) = GetSmoothedFrames(path, avgSize);

            Matrix4x4 matrix = new Matrix4x4(1, 1, 1, 0,
                                             startFrame, midFrame, endFrame, 0,
                                             startFrame * startFrame, midFrame * midFrame, endFrame * endFrame, 0,
                                             0, 0, 0, 1);

            Matrix4x4.Invert(matrix, out var invM);
            matrix = new Matrix4x4(startPoint.X, midPoint.X, endPoint.X, 0,
                                   startPoint.Y, midPoint.Y, endPoint.Y, 0,
                                   0, 0, 0, 0,
                                   0, 0, 0, 0);

            var k = matrix * invM;
            return (new Vector2(k.M13, k.M23), new Vector2(k.M12, k.M22), new Vector2(k.M11, k.M21));
        }

        public static (Vector2 a, Vector2 b, Vector2 c) GetCenterQuadratic(IList<IFramedItem> items)
        {
            int n = items.Count();
            var startPoint = new PointF(0, 0);
            var midPoint = new PointF(0, 0);
            var endPoint = new PointF(0, 0);

            float startFrame = 0;
            float midFrame = 0;
            float endFrame = 0;

            int avgSize = Math.Min(3, n / 3);

            (startPoint, midPoint, endPoint) = GetSmoothedPoints(items, avgSize);
            (startFrame, midFrame, endFrame) = GetSmoothedFrames(items, avgSize);

            Matrix4x4 matrix = new Matrix4x4(1, 1, 1, 0,
                                             startFrame, midFrame, endFrame, 0,
                                             startFrame * startFrame, midFrame * midFrame, endFrame * endFrame, 0,
                                             0, 0, 0, 1);

            Matrix4x4.Invert(matrix, out var invM);
            matrix = new Matrix4x4(startPoint.X, midPoint.X, endPoint.X, 0,
                                   startPoint.Y, midPoint.Y, endPoint.Y, 0,
                                   0, 0, 0, 0,
                                   0, 0, 0, 0);

            var k = matrix * invM;
            return (new Vector2(k.M13, k.M23), new Vector2(k.M12, k.M22), new Vector2(k.M11, k.M21));
        }

        public static (Vector2 a, Vector2 b) GetCenterLineFunc(this IItemPath path)
        {
            int n = path.FramedItems.Count;
            var startPoint = new PointF(0, 0);
            var midPoint = new PointF(0, 0);
            var endPoint = new PointF(0, 0);

            float startFrame = 0;
            float midFrame = 0;
            float endFrame = 0;

            int avgSize = Math.Max(1, Math.Min(3, n / 3));

            (startPoint, midPoint, endPoint) = GetSmoothedPoints(path, avgSize);
            (startFrame, midFrame, endFrame) = GetSmoothedFrames(path, avgSize);

            Matrix4x4 matrix = new Matrix4x4(1, 1, 0, 0,
                                             startFrame, endFrame, 0, 0,
                                             0, 0, 1, 0,
                                             0, 0, 0, 1);

            Matrix4x4.Invert(matrix, out var invM);
            matrix = new Matrix4x4(startPoint.X, endPoint.X, 0, 0,
                                   startPoint.Y, endPoint.Y, 0, 0,
                                   0, 0, 0, 0,
                                   0, 0, 0, 0);

            var k = matrix * invM;
            return (new Vector2(k.M12, k.M22), new Vector2(k.M11, k.M21));
        }
        public static (Vector2 a, Vector2 b) GetCenterLineFunc(IList<IFramedItem> items)
        {
            int n = items.Count;
            var startPoint = new PointF(0, 0);
            var midPoint = new PointF(0, 0);
            var endPoint = new PointF(0, 0);

            float startFrame = 0;
            float midFrame = 0;
            float endFrame = 0;

            int avgSize = Math.Max(1, Math.Min(3, n / 3));

            (startPoint, midPoint, endPoint) = GetSmoothedPoints(items, avgSize);
            (startFrame, midFrame, endFrame) = GetSmoothedFrames(items, avgSize);

            Matrix4x4 matrix = new Matrix4x4(1, 1, 0, 0,
                                             startFrame, endFrame, 0, 0,
                                             0, 0, 1, 0,
                                             0, 0, 0, 1);

            Matrix4x4.Invert(matrix, out var invM);
            matrix = new Matrix4x4(startPoint.X, endPoint.X, 0, 0,
                                   startPoint.Y, endPoint.Y, 0, 0,
                                   0, 0, 0, 0,
                                   0, 0, 0, 0);

            var k = matrix * invM;
            return (new Vector2(k.M12, k.M22), new Vector2(k.M11, k.M21));
        }
        /// <summary>
        ///   Sorts a buffer of items with arbitruary order by frame number.
        /// </summary>
        /// <param name="buffer">
        ///   The buffer to sort.
        /// </param>
        /// <returns>
        ///   Returns a sorted buffer where the first index corresponds to a specific frame number common to all items at that index.
        /// </returns>
        public static IList<IList<IFramedItem>> GroupByFrame(IList<IList<IFramedItem>> buffer, double mergeThreshold, double nameBoost)
        {
            bool alreadySorted = true;
            for (int i = buffer.Count - 1; i >= 0; --i)
            {
                if (!AreAllSameFrame(buffer[i]))
                {
                    alreadySorted = false;
                    break;
                }
            }
            if (alreadySorted)
            {
                return buffer;
            }

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
                    item.MergeIntoFramedItemListSameFrame(ref itemSet, false, mergeThreshold, nameBoost);
                }
            }
            return organizedFrames;
        }

        /// <summary>
        ///   Places the items from an unsorted set into a buffer that is sorted by frame number.
        /// </summary>
        /// <param name="buffer">
        ///   The sorted buffer to add items to.
        /// </param>
        /// <param name="unsortedSet">
        ///   The unsorted set of items to add.
        /// </param>
        /// <returns>
        ///   Returns the merged, sorted set of combined items.
        /// </returns>
        public static IList<IList<IFramedItem>> InsertIntoSortedBuffer(IList<IList<IFramedItem>> buffer, IList<IFramedItem> unsortedSet, double mergeThreshold, double nameBoost)
        {
            if (unsortedSet.Count == 0)
            {
                return buffer;
            }
            /*
            buffer.Add(unsortedSet);
            buffer = GroupByFrame(buffer, mergeThreshold, nameBoost);
            return buffer;*/
            if (buffer.Count == 0)
            {
                buffer.Add(unsortedSet);
                return GroupByFrame(buffer, mergeThreshold, nameBoost);
            }

            bool sameFrame = AreAllSameFrame(unsortedSet);
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
                destList = new List<IList<IFramedItem>>(max - min + 1);
            }
            for (int i = min + destList.Count; i <= max; i++)
            {
                if (sameFrame && unsortedSet[0].Frame.FrameIndex == i)
                {
                    destList.Add(unsortedSet);
                    return destList;
                }
                destList.Add(new List<IFramedItem>());
            }

            HashSet<int> shortcutFrames = new HashSet<int>();
            if (sameFrame)
            {
                int index = unsortedSet[0].Frame.FrameIndex - min;
            }

            foreach (var item in unsortedSet)
            {
                int tgtIndex = item.Frame.FrameIndex - min;
                IList<IFramedItem> dst = destList[tgtIndex];
                if (item.Frame.FrameIndex > bmax)
                {
                    dst.Add(item);
                    continue;
                }

                if (dst.Count == 0)
                {
                    shortcutFrames.Add(tgtIndex);
                }
                else if (dst.Count == 1 && dst[0] is FillerID)
                {
                    dst.RemoveAt(0);
                    shortcutFrames.Add(tgtIndex);
                }
                if (shortcutFrames.Contains(tgtIndex))
                {
                    dst.Add(item);
                }
                else
                {
                    item.MergeIntoFramedItemList(ref dst, false, mergeThreshold, nameBoost);
                }
            }
            return destList;
        }

        /// <summary>
        ///   Fills in an <see cref="IItemPath"/> to ensure that there are no missing frames.
        /// </summary>
        /// <param name="path">
        ///   The path to fill in.
        /// </param>
        /// <param name="frameBuffer">
        ///   The frames available to fill in the path.
        /// </param>
        public static void SealPath(IItemPath path, IList<IList<IFramedItem>> frameBuffer)
        {
            //var orgFrames = GroupByFrame(frameBuffer);
            var orgFrames = new List<IList<IFramedItem>>();
            for (int i = 0; i < frameBuffer.Count; i++)
            {
                orgFrames.Add(new List<IFramedItem>(frameBuffer[i]));
            }
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

        /// <summary>
        ///   Checks the provided list of items and selects up to one to add into the <see cref="IItemPath"/>.
        /// </summary>
        /// <param name="itemsInFrame">
        ///   A set of items, usually all in a single frame, to check.
        /// </param>
        /// <param name="predictor">
        ///   The <see cref="IPathPredictor"/> to use to test the items.
        /// </param>
        /// <param name="itemPath">
        ///   The <see cref="IItemPath"/> to add entries to.
        /// </param>
        /// <param name="similarityThreshold">
        ///   The threshold required to include an <see cref="IFramedItem"/> from the buffer in the path when compared to a predicted location in the same frame.
        /// </param>
        /// <returns>
        ///   Returns <see langword="true"/> if an item was added; <see langword="false"/> otherwise.
        /// </returns>
        public static bool TestAndAdd(IList<IFramedItem> itemsInFrame, IPathPredictor predictor, IItemPath itemPath, double similarityThreshold, bool displayProcess, bool manuallyStep)
        {
            if (itemsInFrame.Count == 0)
            {
                return false;
            }

            int frameIndex = itemsInFrame.First().Frame.FrameIndex;
            Rectangle prediction = predictor.Predict(itemPath, frameIndex);

            double bestSim = similarityThreshold - 1;
            int closestIndex = -1;
            if (displayProcess)
            {
                DrawPrediction(itemsInFrame, prediction, itemPath, frameIndex);
            }

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

        public static void DrawPrediction(IList<IFramedItem> itemsInFrame, Rectangle prediction, IItemPath itemPath, int frameIndex)
        {
            DrawPrediction(itemsInFrame, prediction, itemPath, frameIndex, true, false);
        }

        public static bool IsFillerFrame(IList<IFramedItem> itemsInFrame)
        {
            return itemsInFrame.Count == 1 && itemsInFrame.First().ItemIDs.Count == 1 && itemsInFrame.First().ItemIDs.First() is FillerID;
        }

        public static void DrawPrediction(IList<IFramedItem> itemsInFrame, Rectangle prediction, IItemPath itemPath, int frameIndex, bool displayProcess, bool manuallyStep)
        {
            if (!displayProcess)
            {
                return;
            }

            Mat image = itemsInFrame.First().Frame.FrameData.Clone();

            if (!IsFillerFrame(itemsInFrame))
            {
                for (int i = 0; i < itemsInFrame.Count; i++)
                {
                    var mean = itemsInFrame[i].MeanBounds.RoundRectF();
                    image.Rectangle(new Rect(mean.X, mean.Y, mean.Width, mean.Height), new Scalar(0, 255, 255), 1);
                    DrawParameters(image, itemsInFrame[i], new Scalar(0, 255, 255));
                }
            }

            var nearestItems = itemPath.NearestItems(frameIndex, 10);

            Scalar nColor = new Scalar(0, 255, 0);
            Scalar delta = new Scalar(0, 255 / nearestItems.Count, 0);


            for (int i = 0; i < nearestItems.Count; i++)
            {
                var mean = nearestItems[i].MeanBounds.RoundRectF();
                image.Rectangle(new Rect(mean.X, mean.Y, mean.Width, mean.Height), nColor, 2);
                nColor = new Scalar(nColor.Val0 - delta.Val0, nColor.Val1 - delta.Val1, nColor.Val2 - delta.Val2);
                DrawParameters(image, nearestItems[i], nColor);
            }
            image.Rectangle(new Rect(prediction.X, prediction.Y, prediction.Width, prediction.Height), new Scalar(255, 0, 0), 1);
            Cv2.ImShow("Prediction", image);
            if (manuallyStep)
            {
                Cv2.WaitKey(0);
            }
            else
            {
                Cv2.WaitKey(1);
            }
            image.Dispose();

        }

        private static void DrawParameters(Mat image, IFramedItem item, Color color)
        {
            DrawParameters(image, item, new Scalar(color.R, color.G, color.B));
        }

        private static void DrawParameters(Mat image, IFramedItem item, Scalar color)
        {
            if( item.ItemIDs.Count == 1 && item.ItemIDs.First() is FillerID)
            {
                return;
            }

            int textHeight = 8;
            int offset = 10;
            OpenCvSharp.Point targetPoint = new OpenCvSharp.Point(item.MeanBounds.X, item.MeanBounds.Y - offset);
            if (targetPoint.Y < 10)
            {
                targetPoint = new OpenCvSharp.Point(item.MeanBounds.X, item.MeanBounds.Y + item.MeanBounds.Height + offset + textHeight);
            }

            Cv2.PutText(image, item.ItemIDs[item.HighestConfidenceIndex].ObjName, targetPoint, HersheyFonts.HersheySimplex, textHeight/16.0, color);
        }

        /// <summary>
        ///   Attempts to merge paths together when they share a common <see cref="IItemID"/>.
        /// </summary>
        /// <param name="paths">
        ///   The set of paths to merge together.
        /// </param>
        public static void TryMergePaths(ref IList<IItemPath> paths, double similarityThreshold)
        {
            IList<IItemPath> outPaths = new List<IItemPath>();

            Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries = new Dictionary<IItemPath, (int minFrame, int maxFrame)>();

            foreach (var path in paths)
            {
                boundaries.Add(path, path.GetPathBounds());
            }
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                int minFrame;
                int maxFrame;
                (minFrame, maxFrame) = boundaries[path];

                for (int j = i + 1; j < paths.Count; j++)
                {
                    var tgtPath = paths[j];
                    int tgtMin;
                    int tgtMax;

                    (tgtMin, tgtMax) = boundaries[path];

                    if (tgtMax - maxFrame > 250)
                    {
                        break;
                    }

                    for (int k = Math.Max(tgtMin, minFrame); k < Math.Min(tgtMax, maxFrame); k++)
                    {
                        var tgtFi = GetFramedItemByFrameNumber(tgtPath, k);
                        if (tgtFi == null)
                        {
                            continue;
                        }
                        var srcFi = GetFramedItemByFrameNumber(path, k);
                        if (srcFi == null)
                        {
                            continue;
                        }
                        if (AreFramedItemsMatched(srcFi, tgtFi, similarityThreshold))
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

        public static void MergePaths(Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries, IItemPath path, ref int minFrame, ref int maxFrame, IItemPath srcPath, int srcMin, int srcMax)
        {
            foreach (var fi in srcPath.FramedItems)
            {
                if (fi.IsFiller())
                {
                    continue;
                }

                var dest = GetFramedItemByFrameNumber(path, fi.Frame.FrameIndex);
                if (dest == null)
                {
                    path.FramedItems.Add(fi);
                }
                else
                {
                    if (dest.IsFiller())
                    {
                        dest.ItemIDs.Clear();
                        for (int i = 0; i < fi.ItemIDs.Count; i++)
                        {
                            dest.ItemIDs.Add(fi.ItemIDs[i]);
                        }
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
            }
            maxFrame = Math.Max(maxFrame, srcMax);
            minFrame = Math.Min(minFrame, srcMin);
            boundaries[path] = (minFrame, maxFrame);
        }

        /// <summary>
        ///   Attempts to merge paths together when they share a common <see cref="IItemID"/>.
        /// </summary>
        /// <param name="paths">
        ///   The set of paths to merge together.
        /// </param>
        public static void TryMergePaths(ref IList<IItemPath> paths, IPathPredictor predictor, double similarityThreshold)
        {
            IList<IItemPath> outPaths = new List<IItemPath>();

            Dictionary<IItemPath, (int minFrame, int maxFrame)> boundaries = new Dictionary<IItemPath, (int minFrame, int maxFrame)>();

            foreach (var path in paths)
            {
                boundaries.Add(path, path.GetPathBounds());
            }
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                int minFrame;
                int maxFrame;
                (minFrame, maxFrame) = boundaries[path];

                for (int j = paths.Count - 1; j > i; --j)
                {
                    var tgtPath = paths[j];
                    int tgtMin;
                    int tgtMax;

                    int matchCount = 0;
                    (tgtMin, tgtMax) = boundaries[path];

                    if (tgtMax - maxFrame > 250)
                    {
                        break;
                    }

                    for (int k = Math.Max(tgtMin, minFrame); k < Math.Min(tgtMax, maxFrame); k++)
                    {
                        var tgtFi = GetFramedItemByFrameNumber(tgtPath, k);
                        var srcFi = GetFramedItemByFrameNumber(path, k);
                        if (tgtFi == null)
                        {
                            if (srcFi == null)
                            {
                                Rectangle r1 = predictor.Predict(tgtPath, k);
                                Rectangle r2 = predictor.Predict(path, k);
                                if (r1.IntersectionOverUnion(r2) > 0.85)
                                {
                                    matchCount++;
                                }
                            }
                            else
                            {
                                Rectangle r = predictor.Predict(tgtPath, k);
                                if (r.IntersectionOverUnion(srcFi.MeanBounds.RoundRectF()) > 0.85)
                                {
                                    matchCount++;
                                }
                            }
                        }
                        else if (srcFi == null)
                        {
                            Rectangle r = predictor.Predict(path, k);
                            if (r.IntersectionOverUnion(srcFi.MeanBounds.RoundRectF()) > 0.85)
                            {
                                matchCount++;
                            }
                        }
                        if (matchCount > 5 || AreFramedItemsMatched(srcFi, tgtFi, similarityThreshold))
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
                            paths.RemoveAt(j);
                            break;
                        }
                    }
                }
                outPaths.Add(path);
            }

            paths = outPaths;
        }

        public static bool AreAllSameFrame(IList<IFramedItem> unsortedSet)
        {
            bool sameFrame = true;
            if (unsortedSet == null || unsortedSet.Count == 0)
            {
                return true;
            }
            var f = unsortedSet.First().Frame;
            int frame = f.FrameIndex;
            for (int i = 0; i < unsortedSet.Count; i++)
            {
                var f2 = unsortedSet[i].Frame;
                if (f != f2)
                {
                    if (unsortedSet[i].Frame.FrameIndex != frame)
                    {
                        sameFrame = false;
                        break;
                    }
                    else
                    {
                        // This is unexpected;
                        Console.WriteLine("Duplicate frame found?");
                    }
                }
            }

            return sameFrame;
        }

        public static bool AreFramedItemsMatched(IFramedItem item1, IFramedItem item2, double similarityThreshold)
        {
            if (item1 is null || item2 is null)
            {
                return false;
            }
            if (item1 == item2)
            {
                return true;
            }
            if (item1.Frame.FrameIndex != item2.Frame.FrameIndex || item1.ItemIDs.Last().SourceObject == item2.ItemIDs.Last().SourceObject)
            {
                return false;
            }
            for (int i = 0; i < item1.ItemIDs.Count; i++)
            {
                var id1 = item1.ItemIDs[i];
                for (int j = 0; j < item2.ItemIDs.Count; j++)
                {
                    var id2 = item2.ItemIDs[j];

                    if (id1.Confidence == id2.Confidence && id1.Confidence > 0)
                    {
                        return true;
                    }
                    if (id1.BoundingBox.Location == id2.BoundingBox.Location && id1.BoundingBox.Size == id2.BoundingBox.Size)
                    {
                        return true;
                    }
                }
            }
            if (item1.Similarity(item2.MeanBounds) >= similarityThreshold)
            {
                return true;
            }
            return false;
        }

        public static IList<IList<IFramedItem>> EnsurePathPresenceInCache(IFramedItem framedID, int givenIndex, IList<IList<IFramedItem>> orgFrames)
        {
            int baseFrame = orgFrames[0][0].Frame.FrameIndex;
            if (orgFrames.Count > givenIndex - baseFrame && givenIndex >= baseFrame && !orgFrames[givenIndex - baseFrame].Contains(framedID))
            {
                orgFrames[givenIndex - baseFrame].Add(framedID);
            }
            else if (orgFrames.Count <= givenIndex - baseFrame)
            {
                List<IFramedItem> finalList = new List<IFramedItem>() { framedID };
                orgFrames.Add(finalList);
            }
            else if (givenIndex - baseFrame < 0)
            {
                int numtoadd = baseFrame - givenIndex;

                List<IFramedItem> finalList = new List<IFramedItem>() { framedID };
                IList<IList<IFramedItem>> reorg = new List<IList<IFramedItem>>();
                for (int i = 0; i < numtoadd; i++)
                {
                    if (i == 0)
                    {
                        reorg.Add(finalList);
                    }
                    else
                    {
                        reorg.Add(new List<IFramedItem>());
                    }
                }
                for (int i = 0; i < orgFrames.Count; i++)
                {
                    reorg.Add(orgFrames[i]);
                }
                orgFrames = reorg;
            }

            return orgFrames;
        }

        public static IFramedItem GetFramedItemByFrameNumber(this IItemPath path, int frameNumber)
        {
            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                var fi = path.FramedItems[i];
                if (fi.Frame.FrameIndex == frameNumber)
                {
                    return fi;
                }
            }
            return null;
        }

        public static bool HasSimilarID(IItemID id, IList<IItemID> idlist)
        {
            if (id is FillerID)
            {
                return true;
            }
            if (idlist.Count == 1 && idlist[0] is FillerID)
            {
                return false;
            }
            for (int i = 0; i < idlist.Count; i++)
            {
                if (id.BoundingBox == idlist[i].BoundingBox)
                {
                    return true;
                }
            }
            return false;
        }

        public static void RemoveUsedFrames(IItemPath itemPath, IList<IList<IFramedItem>> orgFrames, ref int startingIndex)
        {
            if (orgFrames == null || orgFrames.Count == 0)
            {
                return;
            }

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
        public static (float startFrame, float midFrame, float endFrame) GetSmoothedFrames(IItemPath path, int averageWindow)
        {
            return GetSmoothedFrames(path.FramedItems, averageWindow);
        }

        public static (float startFrame, float midFrame, float endFrame) GetSmoothedFrames(IList<IFramedItem> items, int averageWindow)
        {
            int n = items.Count;
            var startFrame = 0.0f;
            var midFrame = 0.0f;
            var endFrame = 0.0f;

            int startIndex = 0;
            int midIndex = (n - averageWindow + 1) / 2;
            int endIndex = (n - averageWindow);

            for (int i = 0; i < averageWindow; i++)
            {
                var pt = items[startIndex + i].Frame.FrameIndex;
                startFrame += pt;

                pt = items[midIndex + i].Frame.FrameIndex;
                midFrame += pt;

                pt = items[endIndex + i].Frame.FrameIndex;
                endFrame += pt;
            }

            return (startFrame / averageWindow, midFrame / averageWindow, endFrame / averageWindow);
        }

        public static (PointF startPoint, PointF midPoint, PointF endPoint) GetSmoothedPoints(IItemPath path, int averageWindow)
        {
            return GetSmoothedPoints(path.FramedItems, averageWindow);
        }

        public static (PointF startPoint, PointF midPoint, PointF endPoint) GetSmoothedPoints(IList<IFramedItem> items, int averageWindow)
        {
            int n = items.Count;
            var startPoint = new PointF(0, 0);
            var midPoint = new PointF(0, 0);
            var endPoint = new PointF(0, 0);

            int startIndex = 0;
            int midIndex = (n - averageWindow) / 2;
            int endIndex = (n - averageWindow);

            for (int i = 0; i < averageWindow; i++)
            {
                var pt = items[startIndex + i].MeanBounds.Center();
                startPoint.X += pt.X;
                startPoint.Y += pt.Y;

                pt = items[midIndex + i].MeanBounds.Center();
                midPoint.X += pt.X;
                midPoint.Y += pt.Y;

                pt = items[endIndex + i].MeanBounds.Center();
                endPoint.X += pt.X;
                endPoint.Y += pt.Y;
            }
            startPoint.X /= averageWindow;
            midPoint.X /= averageWindow;
            endPoint.X /= averageWindow;
            startPoint.Y /= averageWindow;
            midPoint.Y /= averageWindow;
            endPoint.Y /= averageWindow;

            return (startPoint, midPoint, endPoint);
        }
    }
}
