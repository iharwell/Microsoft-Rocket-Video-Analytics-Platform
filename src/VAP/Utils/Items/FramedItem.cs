using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Utils.ShapeTools;

namespace Utils.Items
{
    /// <summary>
    ///   The default implementation of the <see cref="IFramedItem" /> interface.
    /// </summary>
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
        public FramedItem( IItemID itemID )
            : this( null, itemID )
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
        public FramedItem(IFrame frame, IItemID itemID )
        {
            ItemIDs = new List<IItemID>();
            Frame = frame;
            ItemIDs.Add( itemID );
        }

        /// <inheritdoc />
        public IFrame Frame { get; set; }

        /// <inheritdoc />
        public IList<IItemID> ItemIDs { get; }

        /// <inheritdoc />
        public byte[] CroppedImageData( int itemIDIndex )
        {
            var item = ItemIDs[itemIDIndex];
            using ( var memoryStream = new MemoryStream( Frame.FrameData ) )
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
        }

        /// <inheritdoc />
        public byte[] TaggedImageData( int itemIDIndex, Color tagColor )
        {
            return TaggedImageData( itemIDIndex, new SolidBrush( tagColor ) );
        }

        /// <inheritdoc />
        public byte[] TaggedImageData( int itemIDIndex, Brush tagColor )
        {
            var item = ItemIDs[itemIDIndex];
            using ( var memoryStream = new MemoryStream( Frame.FrameData ) )
            using ( var image = Image.FromStream( memoryStream ) )
            using ( var canvas = Graphics.FromImage( image ) )
            using ( var pen = new Pen( tagColor, 3 ) )
            {
                canvas.DrawRectangle( pen, item.BoundingBox.X, item.BoundingBox.Y, item.BoundingBox.Width, item.BoundingBox.Height );
                canvas.Flush();

                using ( var memoryStream2 = new MemoryStream() )
                {
                    image.Save( memoryStream2, ImageFormat.Bmp );
                    return memoryStream2.ToArray();
                }
            }
        }

        /// <inheritdoc />
        public double Similarity( Rectangle rect )
        {
            StatisticRectangle sr = new StatisticRectangle(ItemIDs);
            if ( sr.Mean.X <= rect.Right && rect.X <= sr.Mean.Right && sr.Mean.Y <= rect.Bottom && rect.Y <= sr.Mean.Bottom )
            {
                // There is some overlap, so we will give a positive similarity score.
                double ovX = Math.Max( rect.X, sr.Mean.X );
                double ovY = Math.Max( rect.Y, sr.Mean.Y );
                double ovW = Math.Min( rect.Right, sr.Mean.Right ) - ovX;
                double ovH = Math.Min( rect.Bottom, sr.Mean.Bottom ) - ovY;
                RectangleF overlap = new RectangleF( (float)ovX, (float)ovY, (float)ovW, (float)ovH );

                double overlapArea = ovW*ovH;
                // Percent of rect in the mean.
                double overlapPercentRect = overlapArea/(rect.Width*rect.Height);
                double overlapPercentMean = overlapArea/(sr.Mean.Width *sr.Mean.Height);
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

                return -( normalizedDistance1 * normalizedDistance2 ) * sizeFactor;
            }
        }


        public static bool InsertIntoFramedItemList( IList<IFramedItem> framedItems, IItemID itemID, out IFramedItem framedItem, int frameIndex = -1 )
        {
            int bestIndex = 0;
            double bestSimilarity = double.NegativeInfinity;
            for ( int i = 0; i < framedItems.Count; i++ )
            {
                IFramedItem fItem = framedItems[i];
                double sim = double.NegativeInfinity;
                if ( frameIndex>=0 && fItem.Frame.FrameIndex == frameIndex )
                {
                    sim = fItem.Similarity( itemID.BoundingBox );
                    if( sim > bestSimilarity )
                    {
                        bestIndex = i;
                        bestSimilarity = sim;
                    }
                }
            }
            if ( bestSimilarity > 0 )
            {
                framedItem = framedItems[bestIndex];
                framedItem.ItemIDs.Add( itemID );
                return false;
            }
            else
            {
                framedItem = new FramedItem( new Frame(), itemID);
                framedItems.Add( framedItem );
                return true;
            }
        }
    }
}
