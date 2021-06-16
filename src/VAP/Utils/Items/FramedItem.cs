// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using OpenCvSharp;
using Utils.ShapeTools;

namespace Utils.Items
{
    /// <summary>
    ///   The default implementation of the <see cref="IFramedItem" /> interface.
    /// </summary>

    [Serializable]
    [DataContract]
    [KnownType(typeof(Frame))]
    [KnownType(typeof(ItemID))]
    [KnownType(typeof(LineTriggeredItemID))]
    public class FramedItem : IFramedItem
    {
        /// <summary>
        ///   Creates an empty <see cref="FramedItem" />.
        /// </summary>
        public FramedItem()
        {
            ItemIDs = new List<IItemID>();
        }

        /// <summary>
        ///   Creates a <see cref="FramedItem" /> and adds the given <see cref="IItemID" /> to it.
        /// </summary>
        /// <param name="itemID">
        ///   The <see cref="IItemID" /> that is to be framed.
        /// </param>
        public FramedItem(IItemID itemID)
            : this(null, itemID)
        { }

        /// <summary>
        ///   Creates a <see cref="FramedItem" /> and adds the given <see cref="IItemID" /> to it.
        /// </summary>
        /// <param name="frame">
        ///   The <see cref="IFrame" /> that the item was found in.
        /// </param>
        /// <param name="itemID">
        ///   The <see cref="IItemID" /> which identifies the item found in the frame.
        /// </param>
        public FramedItem(IFrame frame, IItemID itemID)
        {
            ItemIDs = new List<IItemID>();
            Frame = frame;
            ItemIDs.Add(itemID);
        }

        /// <inheritdoc />
        [DataMember]
        public IFrame Frame { get; set; }

        /// <inheritdoc />
        [DataMember]
        public IList<IItemID> ItemIDs { get; protected set; }

        private RectangleF? _median;
        private int _medianItemCount;

        public RectangleF MeanBounds
        {
            get
            {
                if ( _medianItemCount != ItemIDs.Count || !_median.HasValue)
                {
                    if( ItemIDs.Count < _medianItemCount || !_median.HasValue)
                    {
                        _median = StatisticRectangle.MeanBox(ItemIDs);
                        _medianItemCount = ItemIDs.Count;
                    }
                    else
                    {
                        _median = StatisticRectangle.UpdateMeanBox(ItemIDs, _median.Value, _medianItemCount);
                        _medianItemCount = ItemIDs.Count;
                    }
                }
                return _median.Value;
            }
        }

        /// <inheritdoc />
        public Mat CroppedImageData(int itemIDIndex)
        {
            /*
            var item = ItemIDs[itemIDIndex];
            using ( var memoryStream = new MemoryStream( Utils.ImageToByteBmp( OpenCvSharp.Extensions.BitmapConverter.ToBitmap( Frame.FrameData ) ) ) )
            using ( var image = Image.FromStream( memoryStream ) )
            {
                Rectangle cropRect = new Rectangle(item.BoundingBox.X,
                                                   item.BoundingBox.Y,
                                                   Math.Min(image.Width-item.BoundingBox.X, item.BoundingBox.Width),
                                                   Math.Min(image.Height-item.BoundingBox.Y, item.BoundingBox.Height));
                Bitmap bmpImage = new Bitmap(image);
                Image croppedImage = bmpImage.Clone(cropRect, bmpImage.PixelFormat);

                using ( var memoryStream2 = new MemoryStream() )
                {
                    croppedImage.Save( memoryStream2, ImageFormat.Bmp );
                    return memoryStream2.ToArray();
                }
            }
            */
            Rectangle bbox = ItemIDs[itemIDIndex].BoundingBox;
            return new Mat(Frame.FrameData, ToRect(bbox));
        }

        /// <inheritdoc />
        public Mat TaggedImageData(int itemIDIndex, Color tagColor)
        {
            Mat output = Frame.FrameData.Clone();
            var color = new Scalar(tagColor.B, tagColor.G, tagColor.R);

            Cv2.Rectangle(output, ToRect(ItemIDs[itemIDIndex].BoundingBox), color, 3);
            return output;
            //return TaggedImageData( itemIDIndex, new SolidBrush( tagColor ) );
        }

        /// <inheritdoc />
        public double Similarity(Rectangle rect)
        {
            if (MeanBounds.X <= rect.Right && rect.X <= MeanBounds.Right && MeanBounds.Y <= rect.Bottom && rect.Y <= MeanBounds.Bottom)
            {
                // There is some overlap, so we will give a positive similarity score.
                double ovX = Math.Max(rect.X, MeanBounds.X);
                double ovY = Math.Max(rect.Y, MeanBounds.Y);
                double ovW = Math.Min(rect.Right, MeanBounds.Right) - ovX;
                double ovH = Math.Min(rect.Bottom, MeanBounds.Bottom) - ovY;

                double overlapArea = ovW * ovH;
                // Percent of rect in the mean.
                double overlapPercentRect = overlapArea / (rect.Width * rect.Height);
                double overlapPercentMean = overlapArea / (MeanBounds.Width * MeanBounds.Height);
                return overlapPercentRect * overlapPercentMean;
            }
            else
            {
                // there is no overlap, so we'll give it a negative similarity score.
                double distSq = PointTools.DistanceSquared(rect.Center(), MeanBounds.Center());
                double diagSq1 = rect.DiagonalSquared();
                double diagSq2 = MeanBounds.DiagonalSquared();
                double normalizedDistance1 = distSq / diagSq1;
                double normalizedDistance2 = distSq / diagSq2;
                double sizeFactor = Math.Max(MeanBounds.Width, rect.Width) / Math.Min(MeanBounds.Width, rect.Width)
                                  * Math.Max(MeanBounds.Height, rect.Height) / Math.Min(MeanBounds.Height, rect.Height);

                return -(normalizedDistance1 * normalizedDistance2) * sizeFactor;
            }
        }


        public static bool InsertIntoFramedItemList(IList<IFramedItem> framedItems, IItemID itemID, out IFramedItem framedItem, int frameIndex = -1)
        {
            int bestIndex = 0;
            double bestSimilarity = double.NegativeInfinity;
            for (int i = 0; i < framedItems.Count; i++)
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
            }
            if (bestSimilarity > 0)
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

        private static Rect ToRect(Rectangle r)
        {
            OpenCvSharp.Point p = new OpenCvSharp.Point(r.X, r.Y);
            OpenCvSharp.Size s = new OpenCvSharp.Size(r.Height, r.Width);
            return new Rect(p, s);
        }

        public static IList<IFramedItem> MergeIntoFramedItemList(IList<IFramedItem> source, ref IList<IFramedItem> target)
        {
            //TODO(iharwell): Add algorithm to merge IFramedItems based on very high similarity scores.
            if (target is null)
            {
                target = source;
            }
            if (source is null)
            {
                return target;
            }

            for (int i = 0; i < source.Count; i++)
            {
                var pids = PositiveIDIndices(source[i].ItemIDs);
                if(pids.Count>0)
                {
                    MergeUsingName(source[i], ref target, BestIDName(source[i].ItemIDs), false);
                }
                else
                {
                    MergeWithoutName(source[i], ref target, false);
                }
            }

            return target;
        }

        public static IList<IFramedItem> MergeIntoFramedItemList(IFramedItem item, ref IList<IFramedItem> target, bool includeFiller)
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
                    MergeUsingName(item, ref target, BestIDName(item.ItemIDs), includeFiller);
                }
                else
                {
                    MergeWithoutName(item, ref target, includeFiller);
                }
            }

            return target;
        }

        public static IList<IFramedItem> MergeIntoFramedItemList(IFramedItem item, ref IList<IFramedItem> target)
        {
            return MergeIntoFramedItemList(item, ref target, false);
        }

        private static IList<IFramedItem> MergeWithoutName(IFramedItem framedItem, ref IList<IFramedItem> target, bool includeFiller)
        {
            IFramedItem bestMatch = null;
            double overlapValue = -999999999999;
            RectangleF srcRect = framedItem.MeanBounds;
            int frameNum = framedItem.Frame.FrameIndex;

            if (framedItem is FillerID)
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
                var tgtRect = item.MeanBounds;
                double s2t = framedItem.Similarity(tgtRect);
                double t2s = item.Similarity(srcRect);
                double avg = (s2t + t2s) / 2;
                if (avg > overlapValue)
                {
                    overlapValue = avg;
                    bestMatch = item;
                }
            }
            if (overlapValue >= 0.4)
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

        private static void RemoveFiller( ref IList<IFramedItem> list)
        {
            bool fillerFound = false;
            if( list.Count>1)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if(list[i] is FillerID)
                    {
                        list.RemoveAt(i);
                    }
                }
            }
        }

        private static IList<IFramedItem> MergeUsingName(IFramedItem framedItem, ref IList<IFramedItem> target, string v, bool includeFiller)
        {
            IFramedItem bestMatch = null;
            double overlapValue = -999999999999;
            RectangleF srcRect = framedItem.MeanBounds;
            int frameNum = framedItem.Frame.FrameIndex;

            if( framedItem is FillerID)
            {
                if( target.Count == 0)
                {
                    target.Add(framedItem);
                }
                return target;
            }

            foreach(IFramedItem item in target)
            {
                if(item.Frame.FrameIndex != frameNum)
                {
                    continue;
                }
                string targetName = BestIDName(item.ItemIDs);
                double mergeBoost = 0.0;
                if (targetName != null && targetName.CompareTo(v) == 0)
                {
                    mergeBoost = 0.15;
                }
                var tgtRect = item.MeanBounds;
                double s2t = framedItem.Similarity(tgtRect);
                double t2s = item.Similarity(srcRect);
                double avg = (s2t + t2s) / 2 + mergeBoost;
                if (avg > overlapValue)
                {
                    overlapValue = avg;
                    bestMatch = item;
                }
            }
            if (overlapValue >= 0.4)
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

        private static IList<int> PositiveIDIndices( IList<IItemID> itemIDs )
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < itemIDs.Count; i++)
            {
                if(itemIDs[i].Confidence > 0 && itemIDs[i].ObjName != null)
                {
                    indices.Add(i);
                }
            }
            return indices;
        }

        private static string BestIDName(IList<IItemID> itemIDs)
        {
            string name = null;
            double bestConfidence = 0;
            for (int i = 0; i < itemIDs.Count; i++)
            {
                if (itemIDs[i].Confidence > bestConfidence)
                {
                    name = itemIDs[i].ObjName;
                    bestConfidence = itemIDs[i].Confidence;
                }
            }
            return name;
        }
    }
}
