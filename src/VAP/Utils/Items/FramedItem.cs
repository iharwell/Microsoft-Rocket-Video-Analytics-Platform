﻿// Copyright (c) Microsoft Corporation.
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
    [KnownType( typeof( Frame ) )]
    [KnownType( typeof( ItemID ) )]
    [KnownType( typeof( LineTriggeredItemID ) )]
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
            StatisticRectangle sr = new StatisticRectangle(ItemIDs);
            if (sr.Mean.X <= rect.Right && rect.X <= sr.Mean.Right && sr.Mean.Y <= rect.Bottom && rect.Y <= sr.Mean.Bottom)
            {
                // There is some overlap, so we will give a positive similarity score.
                double ovX = Math.Max(rect.X, sr.Mean.X);
                double ovY = Math.Max(rect.Y, sr.Mean.Y);
                double ovW = Math.Min(rect.Right, sr.Mean.Right) - ovX;
                double ovH = Math.Min(rect.Bottom, sr.Mean.Bottom) - ovY;
                RectangleF overlap = new RectangleF((float)ovX, (float)ovY, (float)ovW, (float)ovH);

                double overlapArea = ovW * ovH;
                // Percent of rect in the mean.
                double overlapPercentRect = overlapArea / (rect.Width * rect.Height);
                double overlapPercentMean = overlapArea / (sr.Mean.Width * sr.Mean.Height);
                return overlapPercentRect * overlapPercentMean;
            }
            else
            {
                // there is no overlap, so we'll give it a negative similarity score.
                double distSq = PointTools.DistanceSquared(rect.Center(), sr.Mean.Center());
                double diagSq1 = rect.DiagonalSquared();
                double diagSq2 = sr.Mean.DiagonalSquared();
                double normalizedDistance1 = distSq / diagSq1;
                double normalizedDistance2 = distSq / diagSq2;
                double sizeFactor = Math.Max(sr.Mean.Width, rect.Width) / Math.Min(sr.Mean.Width, rect.Width)
                                  * Math.Max(sr.Mean.Height, rect.Height) / Math.Min(sr.Mean.Height, rect.Height);

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
                double sim = double.NegativeInfinity;
                if (frameIndex >= 0 && fItem.Frame.FrameIndex == frameIndex)
                {
                    sim = fItem.Similarity(itemID.BoundingBox);
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
            if(source is null )
            {
                return target;
            }

            for (int i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);
            }

            return target;
        }
    }
}
