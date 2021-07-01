// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Wrapper.Yolo.Model;

namespace Wrapper.Yolo
{
    public class YoloTracking
    {
        private readonly YoloWrapper _yoloWrapper;
        //private Point _trackingObject = new Point(0, 0);
        private readonly int _maxDistance;
        public int _index;

        //distance-based validation
        public YoloTracking(YoloWrapper yoloWrapper, int maxDistance = int.MaxValue)
        {
            this._yoloWrapper = yoloWrapper;
            this._maxDistance = maxDistance;
        }

        /*public void SetTrackingObject(Point trackingObject)
        {
            this._trackingObject = trackingObject;
        }*/

        public List<YoloTrackingItem> Analyse(byte[] imageData, ISet<string> category, Color bboxColor)
        {
            var items = this._yoloWrapper.Track(imageData);
            if (items == null || !items.Any())
            {
                return null;
            }

            var probableObject = FindAllMatchIgnoreDistance(items, category);
            if (probableObject.Count == 0)
            {
                return null;
            }

            var validObjects = new List<YoloTrackingItem>();
            foreach (var obj in probableObject)
            {
                //var taggedImageData = this.DrawImage(imageData, obj, bboxColor);
                //var croppedImageData = this.CropImage(imageData, obj);

                validObjects.Add(new YoloTrackingItem(obj, this._index, null, null));
                this._index++;
            }
            return validObjects;
        }

        public List<YoloTrackingItem> AnalyseUnmanaged(IntPtr imageData, int dataSize, ISet<string> category, Point trackingObject, Color bboxColor)
        {
            var items = this._yoloWrapper.TrackUnmanaged(imageData, dataSize);
            if (items == null || !items.Any())
            {
                return null;
            }

            var probableObject = FindAllMatch(items, _maxDistance, trackingObject, category);
            if (probableObject.Count == 0)
            {
                return null;
            }

            var validObjects = new List<YoloTrackingItem>();
            foreach (var obj in probableObject)
            {
                //var taggedImageData = this.DrawImage(imageData, obj, bboxColor);
                //var croppedImageData = this.CropImage(imageData, obj);

                validObjects.Add(new YoloTrackingItem(obj, this._index, null, null));
                this._index++;
            }
            return validObjects;
        }
        public List<YoloTrackingItem> AnalyseUnmanagedNoDist(IntPtr imageData, int dataSize, ISet<string> category)
        {
            var items = this._yoloWrapper.TrackUnmanaged(imageData, dataSize);
            if (items == null || !items.Any())
            {
                return null;
            }

            var probableObject = FindAllMatchIgnoreDistance(items, category);
            if (probableObject.Count == 0)
            {
                return null;
            }

            var validObjects = new List<YoloTrackingItem>();
            foreach (var obj in probableObject)
            {
                //var taggedImageData = this.DrawImage(imageData, obj, bboxColor);
                //var croppedImageData = this.CropImage(imageData, obj);

                validObjects.Add(new YoloTrackingItem(obj, this._index, null, null));
                this._index++;
            }
            return validObjects;
        }

        private static YoloItem FindBestMatch(IEnumerable<YoloItem> items, Point trackingObject, int maxDistance)
        {
            //var distanceItems = items.Select(o => new { Distance = this.Distance(o.Center(), this._trackingObject), Item = o }).Where(o => o.Distance <= maxDistance).OrderBy(o => o.Distance);

            var distanceItems = from item in items
                                orderby Distance(item.Center(), trackingObject)
                                select item;


            var bestMatch = distanceItems.FirstOrDefault();
            return bestMatch;
        }

        //find all match based on distance
        private static List<YoloItem> FindAllMatch(IEnumerable<YoloItem> items, int maxDistance, Point trackingObject, ISet<string> category)
        {
            //var distanceItems = items.Select(o => new{ Category = o.Type, Distance = this.Distance(o.Center(), this._trackingObject), Item = o }).Where(o => (category.Count == 0 || category.Contains(o.Category))&& o.Distance <= maxDistance).OrderBy(o => o.Distance);

            var distanceItems = from item in items
                                where category.Count == 0 || category.Contains(item.Type)
                                let itemDistance = Distance(item.Center(), trackingObject)
                                where itemDistance <= maxDistance
                                orderby itemDistance
                                select item;

            var yItems = new List<YoloItem>(distanceItems);
            /*foreach (var item in distanceItems)
            {
                YoloItem yItem = item;
                yItems.Add(yItem);
            }*/
            return yItems;
        }


        //find all match based on distance
        private static List<YoloItem> FindAllMatchIgnoreDistance(IEnumerable<YoloItem> items, ISet<string> category)
        {
            //var distanceItems = items.Select(o => new{ Category = o.Type, Distance = this.Distance(o.Center(), this._trackingObject), Item = o }).Where(o => (category.Count == 0 || category.Contains(o.Category))&& o.Distance <= maxDistance).OrderBy(o => o.Distance);

            var distanceItems = from item in items
                                where category.Count == 0 || category.Contains(item.Type)
                                select item;

            var yItems = new List<YoloItem>(distanceItems);
            /*foreach (var item in distanceItems)
            {
                YoloItem yItem = item;
                yItems.Add(yItem);
            }*/
            return yItems;
        }

        private static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Pow2(p2.X - p1.X) + Pow2(p2.Y - p1.Y));
        }

        private static double Pow2(double x)
        {
            return x * x;
        }

        private static byte[] DrawImage(byte[] imageData, YoloItem item, Brush color)
        {
            using var memoryStream = new MemoryStream(imageData);
            using var image = Image.FromStream(memoryStream);
            using var canvas = Graphics.FromImage(image);
            using var pen = new Pen(color, 3);
            canvas.DrawRectangle(pen, item.X, item.Y, item.Width, item.Height);
            canvas.Flush();

            using var memoryStream2 = new MemoryStream();
            image.Save(memoryStream2, ImageFormat.Bmp);
            return memoryStream2.ToArray();
        }

        private static byte[] CropImage(byte[] imageData, YoloItem item)
        {
            using var memoryStream = new MemoryStream(imageData);
            using var image = Image.FromStream(memoryStream);
            var cropRect = new Rectangle(item.X, item.Y, Math.Min(image.Width - item.X, item.Width), Math.Min(image.Height - item.Y, item.Height));
            var bmpImage = new Bitmap(image);
            Image croppedImage = bmpImage.Clone(cropRect, bmpImage.PixelFormat);

            using var memoryStream2 = new MemoryStream();
            croppedImage.Save(memoryStream2, ImageFormat.Bmp);
            return memoryStream2.ToArray();
        }

        public static double GetDistance(Point p1, Point trackingObject)
        {
            return Distance(p1, trackingObject);
        }
    }
}
