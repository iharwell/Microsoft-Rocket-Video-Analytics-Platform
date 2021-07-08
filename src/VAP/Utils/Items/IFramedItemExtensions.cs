// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Utils.ShapeTools;

namespace Utils.Items
{
    public static class IFramedItemExtensions
    {
        /// <summary>
        ///   Calculates a similarity score between the provided rectangle and the items in this <see cref="IFramedItem" />.
        /// </summary>
        /// <param name="item">
        ///   The <see cref="IFramedItem" /> to calculate the score for.
        /// </param>
        /// <param name="rect">
        ///   The rectangle under consideration.
        /// </param>
        /// <returns>
        ///   Returns a value no greater than 1, with greater numbers representing a closer match
        ///   and positive values indicating some degree of overlap.
        /// </returns>
        public static double Similarity(this IFramedItem item, Rectangle rect)
        {
            if (rect.Width == 0)
            {
                rect.Width = 1;
            }
            if (rect.Height == 0)
            {
                rect.Height = 1;
            }
            StatisticRectangle sr = new StatisticRectangle(item.ItemIDs);

            var median = sr.Median;
            if (median.Width == 0)
            {
                median = new RectangleF(median.Location, new SizeF(1, median.Height));
            }
            if (median.Height == 0)
            {
                median = new RectangleF(median.Location, new SizeF(median.Width, 1));
            }
            if (sr.Median.X <= rect.Right && rect.X <= sr.Median.Right && sr.Median.Y <= rect.Bottom && rect.Y <= sr.Median.Bottom)
            {
                // There is some overlap, so we will give a positive similarity score.
                double ovX = Math.Max(rect.X, median.X);
                double ovY = Math.Max(rect.Y, median.Y);
                double ovW = Math.Min(rect.Right, median.Right) - ovX;
                double ovH = Math.Min(rect.Bottom, median.Bottom) - ovY;

                double overlapArea = ovW * ovH;
                double srArea = median.Width * median.Height;
                double rectArea = rect.Width * rect.Height;
                return (overlapArea) / (srArea + rectArea - overlapArea);
            }
            else
            {
                // there is no overlap, so we'll give it a negative similarity score, but faster
                return double.MinValue;
            }
        }

        /// <summary>
        ///   Calculates a similarity score between the provided rectangle and the items in this <see cref="IFramedItem" />.
        /// </summary>
        /// <param name="item">
        ///   The <see cref="IFramedItem" /> to calculate the score for.
        /// </param>
        /// <param name="rect">
        ///   The rectangle under consideration.
        /// </param>
        /// <returns>
        ///   Returns a value no greater than 1, with greater numbers representing a closer match
        ///   and positive values indicating some degree of overlap.
        /// </returns>
        public static double SimilarityFull(this IFramedItem item, RectangleF rect)
        {
            if (rect.Width == 0)
            {
                rect.Width = 1;
            }
            if (rect.Height == 0)
            {
                rect.Height = 1;
            }
            //StatisticRectangle sr = new StatisticRectangle(item.ItemIDs);

            var median = item.MeanBounds;
            if (median.Width == 0)
            {
                median = new RectangleF(median.Location, new SizeF(1, median.Height));
            }
            if (median.Height == 0)
            {
                median = new RectangleF(median.Location, new SizeF(median.Width, 1));
            }
            if (median.X <= rect.Right && rect.X <= median.Right && median.Y <= rect.Bottom && rect.Y <= median.Bottom)
            {
                // There is some overlap, so we will give a positive similarity score.
                double ovX = Math.Max(rect.X, median.X);
                double ovY = Math.Max(rect.Y, median.Y);
                double ovW = Math.Min(rect.Right, median.Right) - ovX;
                double ovH = Math.Min(rect.Bottom, median.Bottom) - ovY;

                double overlapArea = ovW * ovH;
                double srArea = median.Width * median.Height;
                double rectArea = rect.Width * rect.Height;
                return (overlapArea) / (srArea + rectArea - overlapArea);
            }
            else
            {
                // there is no overlap, so we'll give it a negative similarity score.
                double distSq = PointTools.DistanceSquared(rect.Center(), median.Center());
                double diagSq1 = rect.DiagonalSquared();
                double diagSq2 = median.DiagonalSquared();
                double normalizedDistance1 = distSq / diagSq1;
                double normalizedDistance2 = distSq / diagSq2;
                double sizeFactor = Math.Max(median.Width, rect.Width) / Math.Min(median.Width, rect.Width)
                                  * Math.Max(median.Height, rect.Height) / Math.Min(median.Height, rect.Height);

                return -(normalizedDistance1 * normalizedDistance2) * sizeFactor;
            }
        }


        /// <summary>
        ///   Calculates a similarity score between the provided rectangle and the items in this <see cref="IFramedItem" />.
        /// </summary>
        /// <param name="item">
        ///   The <see cref="IFramedItem" /> to calculate the score for.
        /// </param>
        /// <param name="rect">
        ///   The rectangle under consideration.
        /// </param>
        /// <returns>
        ///   Returns a value no greater than 1, with greater numbers representing a closer match
        ///   and positive values indicating some degree of overlap.
        /// </returns>
        public static double Similarity(this IFramedItem item, RectangleF rect)
        {
            if (rect.Width == 0)
            {
                rect.Width = 1;
            }
            if (rect.Height == 0)
            {
                rect.Height = 1;
            }
            //StatisticRectangle sr = new StatisticRectangle(item.ItemIDs);

            var median = item.MeanBounds;
            if (median.Width == 0)
            {
                median = new RectangleF(median.Location, new SizeF(1, median.Height));
            }
            if (median.Height == 0)
            {
                median = new RectangleF(median.Location, new SizeF(median.Width, 1));
            }
            if (median.X <= rect.Right && rect.X <= median.Right && median.Y <= rect.Bottom && rect.Y <= median.Bottom)
            {
                // There is some overlap, so we will give a positive similarity score.
                double ovX = Math.Max(rect.X, median.X);
                double ovY = Math.Max(rect.Y, median.Y);
                double ovW = Math.Min(rect.Right, median.Right) - ovX;
                double ovH = Math.Min(rect.Bottom, median.Bottom) - ovY;

                double overlapArea = ovW * ovH;
                double srArea = median.Width * median.Height;
                double rectArea = rect.Width * rect.Height;
                /*
                double overlapPercentRect = overlapArea/(rect.Width*rect.Height);
                double overlapPercentMean = overlapArea/(sr.Median.Width *sr.Median.Height);
                return overlapPercentRect * overlapPercentMean;*/
                return (overlapArea) / (srArea + rectArea - overlapArea);
            }
            else
            {
                return float.MinValue;
            }
        }

        /// <summary>
        ///   Inserts a <see cref="IItemID" /> into a list of existing <see cref="IFramedItem" />
        ///   objects, either by adding the <see cref="IItemID" /> to an existing entry, or by
        ///   creating a new entry if no match is found.
        /// </summary>
        /// <param name="itemID">
        ///   The <see cref="IItemID" /> to insert.
        /// </param>
        /// <param name="framedItems">
        ///   The list of <see cref="IFramedItem" /> objects to insert the item into.
        /// </param>
        /// <param name="framedItem">
        ///   Outputs the specific <see cref="IFramedItem" /> that the provided
        ///   <see cref="IItemID" /> was added to. This object will both exist in the list and
        ///   contain the provided <see cref="IItemID" />.
        /// </param>
        /// <param name="frameIndex">
        ///   The index in the list that the provided <see cref="IItemID" /> was added to.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if a new <see cref="IFramedItem" /> was created to add the
        ///   item; <see langword="false" /> if the item was appended to an existing entry.
        /// </returns>
        public static bool InsertIntoFramedItemList(this IItemID itemID, IList<IFramedItem> framedItems, out IFramedItem framedItem, int frameIndex)
        {
            int bestIndex = 0;
            IFramedItem bestItem = null;
            double bestSimilarity = double.NegativeInfinity;

            if (itemID is FillerID)
            {
                framedItem = null;
                return false;
            }


            var fiInSameFrame = FilterByFrame(framedItems, frameIndex);

            foreach (var item in fiInSameFrame)
            {
                var sim = item.Similarity(itemID.BoundingBox);
                if (sim > bestSimilarity)
                {
                    bestItem = item;
                    bestSimilarity = sim;
                }
            }

            /*for (int i = 0; i < framedItems.Count; i++)
            {
                IFramedItem fItem = framedItems[i];
                if (frameIndex >= 0 && fItem.Frame.FrameIndex == frameIndex)
                {
                    var sim = fItem.Similarity(itemID.BoundingBox);
                    if (sim > bestSimilarity)
                    {
                        bestIndex = i;
                        bestSimilarity = sim;
                    }
                }
            }*/
            if (bestSimilarity > 0.05)
            {
                framedItem = framedItems[bestIndex];
                framedItem.ItemIDs.Add(itemID);
                return false;
            }
            else
            {
                framedItem = new FramedItem(new Frame(), itemID);
                framedItems.Add(framedItem);
                return true;
            }
        }

        public static IList<IFramedItem> MergeIntoFramedItemList(this IFramedItem item, ref IList<IFramedItem> target, bool includeFiller, double mergeThreshold, double nameBoost)
        {
            //TODO(iharwell): Add algorithm to merge IFramedItems based on very high similarity scores.
            if (item is null)
            {
                return target;
            }

            if (target.Count == 0)
            {
                target.Add(item);
            }
            else
            {
                var pids = PositiveIDIndices(item.ItemIDs);
                if (pids.Count > 0)
                {
                    MergeUsingName(item, ref target, item.BestIDName(), includeFiller, mergeThreshold, nameBoost);
                }
                else
                {
                    MergeWithoutName(item, ref target, includeFiller, mergeThreshold);
                }
            }

            return target;
        }


        public static IList<IFramedItem> MergeIntoFramedItemListSameFrame(this IFramedItem item, ref IList<IFramedItem> target, bool includeFiller, double mergeThreshold, double nameBoost)
        {
            //TODO(iharwell): Add algorithm to merge IFramedItems based on very high similarity scores.
            if (item is null)
            {
                return target;
            }

            if (target.Count == 0)
            {
                target.Add(item);
            }
            else
            {
                var pids = PositiveIDIndices(item.ItemIDs);
                if (pids.Count > 0)
                {
                    MergeUsingName(item, ref target, item.BestIDName(), includeFiller, mergeThreshold, nameBoost);
                }
                else
                {
                    MergeWithoutNameSameFrame(item, ref target, includeFiller, mergeThreshold);
                }
            }

            return target;
        }

        public static IList<IFramedItem> MergeUsingName(this IFramedItem framedItem, ref IList<IFramedItem> target, string v, bool includeFiller, double mergeThreshold, double nameBoost)
        {
            IFramedItem bestMatch = null;
            double overlapValue = -999999999999;
            RectangleF srcRect = framedItem.MeanBounds;
            int frameNum = framedItem.Frame.FrameIndex;

            bool alreadyExists = false;

            if (framedItem.ItemIDs.Count == 1 && framedItem.ItemIDs[0] is FillerID)
            {
                if (target.Count == 0)
                {
                    target.Add(framedItem);
                }
                return target;
            }

            foreach (IFramedItem item in target)
            {
                if (item.Frame.FrameIndex != frameNum)
                {
                    continue;
                }
                if (item == framedItem)
                {
                    alreadyExists = true;
                    return target;
                }
                string targetName = item.BestIDName();
                double mergeBoost = 0.0;
                if (targetName != null && targetName.CompareTo(v) == 0)
                {
                    mergeBoost = nameBoost;
                }
                var tgtRect = item.MeanBounds;
                double s2t = framedItem.Similarity(tgtRect);
                double avg = s2t + mergeBoost;
                if (avg > overlapValue)
                {
                    overlapValue = avg;
                    bestMatch = item;
                }
            }

            if (overlapValue >= mergeThreshold)
            {
                for (int i = 0; i < framedItem.ItemIDs.Count; i++)
                {
                    bestMatch.ItemIDs.Add(framedItem.ItemIDs[i]);
                }
            }
            else
            {
                // No match found
                target.Add(framedItem);
            }
            RemoveFiller(ref target);
            return target;
        }
        public static IList<IFramedItem> MergeWithoutName(this IFramedItem framedItem, ref IList<IFramedItem> target, bool includeFiller, double mergeThreshold)
        {
            IFramedItem bestMatch = null;
            double overlapValue = -999999999999;
            RectangleF srcRect = framedItem.MeanBounds;
            var singleIDs = framedItem.ItemIDs;
            int frameNum = framedItem.Frame.FrameIndex;

            if (singleIDs.Count == 1 && singleIDs[0] is FillerID)
            {
                if (target.Count == 0)
                {
                    target.Add(framedItem);
                }
                return target;
            }

            foreach (IFramedItem item in target)
            {
                if (item.Frame.FrameIndex != frameNum || item == framedItem)
                {
                    continue;
                }

                var tgtRect = item.MeanBounds;
                double s2t = framedItem.Similarity(tgtRect);
                if (s2t > overlapValue)
                {
                    overlapValue = s2t;
                    bestMatch = item;
                }
            }
            if (overlapValue >= mergeThreshold)
            {
                var bestIDs = bestMatch.ItemIDs;
                for (int i = 0; i < singleIDs.Count; i++)
                {
                    bestIDs.Add(singleIDs[i]);
                }
            }
            else
            {
                // No match found
                target.Add(framedItem);
            }
            RemoveFiller(ref target);
            return target;
        }

        private static IList<IFramedItem> MergeWithoutNameSameFrame(this IFramedItem framedItem, ref IList<IFramedItem> target, bool includeFiller, double mergeThreshold)
        {
            IFramedItem bestMatch = null;
            double overlapValue = -999999999999;
            RectangleF srcRect = framedItem.MeanBounds;
            var singleIDs = framedItem.ItemIDs;
            int frameNum = framedItem.Frame.FrameIndex;

            if (singleIDs.Count == 1 && singleIDs[0] is FillerID)
            {
                if (target.Count == 0)
                {
                    target.Add(framedItem);
                }
                return target;
            }

            foreach (IFramedItem item in target)
            {
                var tgtRect = item.MeanBounds;
                double s2t = framedItem.Similarity(tgtRect);
                if (s2t > overlapValue)
                {
                    overlapValue = s2t;
                    bestMatch = item;
                }
            }
            if (overlapValue >= mergeThreshold)
            {
                var bestIDs = bestMatch.ItemIDs;
                for (int i = 0; i < singleIDs.Count; i++)
                {
                    bestIDs.Add(singleIDs[i]);
                }
            }
            else
            {
                // No match found
                target.Add(framedItem);
            }
            RemoveFiller(ref target);
            return target;
        }
        public static string BestIDName(this IFramedItem item)
        {
            string name = null;
            double bestConfidence = 0;
            for (int i = 0; i < item.ItemIDs.Count; i++)
            {
                if (item.ItemIDs[i].Confidence > bestConfidence)
                {
                    name = item.ItemIDs[i].ObjName;
                    bestConfidence = item.ItemIDs[i].Confidence;
                }
            }
            return name;
        }
        public static IList<int> PositiveIDIndices(this IFramedItem item)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < item.ItemIDs.Count; i++)
            {
                if (item.ItemIDs[i].Confidence > 0 && item.ItemIDs[i].ObjName != null)
                {
                    indices.Add(i);
                }
            }
            return indices;
        }

        public static IList<int> PositiveIDIndices(IList<IItemID> itemIDs)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < itemIDs.Count; i++)
            {
                if (itemIDs[i].Confidence > 0 && itemIDs[i].ObjName != null)
                {
                    indices.Add(i);
                }
            }
            return indices;
        }

        private static IEnumerable<IFramedItem> FilterByFrame(IList<IFramedItem> framedItems, int frameIndex)
        {
            return from item in framedItems
                   where item.Frame.FrameIndex == frameIndex
                   select item;
        }
        public static void RemoveFiller(ref IList<IFramedItem> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var ids = list[i].ItemIDs;
                if (ids.Count > 1)
                {
                    IItemID fillerID = null;
                    for (int j = 0; j < ids.Count; j++)
                    {
                        if (ids[j] is FillerID)
                        {
                            if (fillerID == null)
                            {
                                fillerID = ids[j];
                            }
                            ids.RemoveAt(j);
                        }
                    }
                    if(ids.Count == 0 && fillerID != null)
                    {
                        ids.Add(fillerID);
                    }
                }
            }
        }
    }
}
