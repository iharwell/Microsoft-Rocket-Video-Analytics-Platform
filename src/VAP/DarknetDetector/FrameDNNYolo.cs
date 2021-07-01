// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Config;
using DNNDetector.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Utils;
using Utils.Config;
using Utils.Items;
using Utils.ShapeTools;
using Wrapper.Yolo;
using Wrapper.Yolo.Model;
using Point = System.Drawing.Point;
namespace DarknetDetector
{
    public class FrameDNNDarknet : IDNNAnalyzer
    {
        private readonly YoloWrapper _frameYolo;
        private readonly YoloTracking _frameYoloTracking;
        private readonly List<(string key, LineSegment coordinates)> _lines;

        //distance-based validation
        public FrameDNNDarknet(string modelConfig, DNNMode dnnMode, double rFactor)
        {
            var configurationDetector = new ConfigurationDetector(modelConfig);
            _frameYolo = new YoloWrapper(configurationDetector.Detect(), dnnMode);
            _frameYoloTracking = new YoloTracking(_frameYolo, Convert.ToInt32(DNNConfig.ValidRange * rFactor));
        }

        //overlap ratio-based validation
        public FrameDNNDarknet(string modelConfig, DNNMode dnnMode, List<(string key, LineSegment coordinates)> lines)
        {
            var configurationDetector = new ConfigurationDetector(modelConfig);
            _frameYolo = new YoloWrapper(configurationDetector.Detect(), dnnMode);
            _frameYoloTracking = new YoloTracking(_frameYolo);
            _lines = lines;
        }

        //without validation i.e., output all yolo detections
        public FrameDNNDarknet(string modelConfig, DNNMode dnnMode)
        {
            var configurationDetector = new ConfigurationDetector(modelConfig);
            _frameYolo = new YoloWrapper(configurationDetector.Detect(), dnnMode);
            _frameYoloTracking = new YoloTracking(_frameYolo);
        }

        /*public void SetTrackingPoint(Point trackingObject)
        {
            _frameYoloTracking.SetTrackingObject(trackingObject);
        }*/

        public unsafe IEnumerable<YoloTrackingItem> Analyse(Mat imgByte, ISet<string> category, Point trackingPoint, Color bboxColor)
        {
            byte[] imgBuffer = imgByte.ToBytes(".bmp");
            IEnumerable<YoloTrackingItem> yoloItems;
            fixed (byte* ptr = imgBuffer)
            {
                yoloItems = _frameYoloTracking.AnalyseUnmanaged((IntPtr)ptr, imgBuffer.Length, category, trackingPoint, bboxColor);
            }
            return yoloItems;
        }

        public unsafe IEnumerable<YoloTrackingItem> AnalyseRaw(Mat imgByte, ISet<string> category)
        {
            byte[] imgBuffer = imgByte.ToBytes(".bmp");
            IEnumerable<YoloTrackingItem> yoloItems;
            fixed (byte* ptr = imgBuffer)
            {
                yoloItems = _frameYoloTracking.AnalyseUnmanagedNoDist((IntPtr)ptr, imgBuffer.Length, category);
            }
            return yoloItems;
        }
        public unsafe IEnumerable<IItemID> Analyze(Mat imgByte, ISet<string> category, object sourceObject)
        {
            byte[] imgBuffer = imgByte.ToBytes(".bmp");
            IEnumerable<YoloTrackingItem> yoloItems;
            fixed (byte* ptr = imgBuffer)
            {
                yoloItems = _frameYoloTracking.AnalyseUnmanagedNoDist((IntPtr)ptr, imgBuffer.Length, category);
            }
            if (yoloItems == null)
            {
                return null;
            }
            List<IItemID> items = new List<IItemID>();
            foreach (var item in yoloItems)
            {
                ItemID id = new ItemID(item.BBox, item.ObjId, item.Type, item.Confidence, item.TrackId, nameof(FrameDNNDarknet))
                {
                    SourceObject = sourceObject
                };
                items.Add(id);
            }
            return items;
        }

        public List<YoloTrackingItem> AnalyzeAndDetect(Mat imgByte, ISet<string> category, int lineID, Color bboxColor, double min_score_for_linebbox_overlap, Point trackingPoint, int frameIndex = 0)
        {
            byte[] imgBuffer = imgByte.ToBytes(".bmp");
            var yoloItems = Analyse(imgByte, category, trackingPoint, bboxColor);
            return OverlapVal(imgBuffer, yoloItems, lineID, bboxColor, min_score_for_linebbox_overlap);
        }

        public List<YoloTrackingItem> CachedDetect(Mat imgByte, IEnumerable<YoloTrackingItem> yoloItems, int lineID, Color bboxColor, double min_score_for_linebbox_overlap, int frameIndex = 0)
        {
            byte[] imgBuffer = imgByte.ToBytes(".bmp");
            return OverlapVal(imgBuffer, yoloItems, lineID, bboxColor, min_score_for_linebbox_overlap);
        }
        public List<IItemID> CachedDetect(Mat imgByte, IEnumerable<IItemID> cachedItems, int lineID, Color bboxColor, double min_score_for_linebbox_overlap, int frameIndex = 0)
        {
            byte[] imgBuffer = imgByte.ToBytes(".bmp");
            return OverlapVal(imgBuffer, cachedItems, lineID, min_score_for_linebbox_overlap);
        }

        public static IDictionary<YoloTrackingItem, bool> GetOverlappingItems(IEnumerable<YoloTrackingItem> yoloItems, LineSegment line, double overlapThreshold)
        {
            Dictionary<YoloTrackingItem, bool> results = new Dictionary<YoloTrackingItem, bool>();
            foreach (var item in yoloItems)
            {
                double overlap = Utils.Utils.CheckLineBboxOverlapRatio(line, item.BBox);
                results.Add(item, overlap >= overlapThreshold);
            }
            return results;
        }

        private List<YoloTrackingItem> OverlapVal(byte[] imgByte, IEnumerable<YoloTrackingItem> yoloItems, int lineID, Color bboxColor, double min_score_for_linebbox_overlap)
        {
            var image = Image.FromStream(new MemoryStream(imgByte)); // to filter out bbox larger than the frame

            if (yoloItems == null) return null;

            //run overlap ratio-based validation
            if (_lines != null)
            {
                List<YoloTrackingItem> validObjects = new List<YoloTrackingItem>();

                ////--------------Output images with all bboxes----------------
                //byte[] canvas = new byte[imgByte.Length];
                //canvas = imgByte;
                //YoloTrackingItem[] dbItems = yoloItems.ToArray();
                //double dbOverlap;
                //foreach (var dbItem in dbItems)
                //{
                //    dbOverlap = Utils.Utils.checkLineBboxOverlapRatio(lines[lineID].Item2, dbItem.X, dbItem.Y, dbItem.Width, dbItem.Height);
                //    canvas = Utils.Utils.DrawImage(canvas, dbItem.X, dbItem.Y, dbItem.Width, dbItem.Height, bboxColor, dbOverlap.ToString());
                //}
                //File.WriteAllBytes(@OutputFolder.OutputFolderAll + $"tmp-{frameIndex}-{lines[lineID].Item1}.jpg", canvas);
                ////--------------Output images with all bboxes----------------

                /*var overlapItems = yoloItems
                                    .Select(
                                        o => new
                                        {
                                            Overlap = Utils.Utils.CheckLineBboxOverlapRatio(_lines[lineID].coordinates, o.X, o.Y, o.Width, o.Height),
                                            Bbox_x = o.X + o.Width,
                                            Bbox_y = o.Y + o.Height,
                                            Distance = this.Distance(_lines[lineID].coordinates, o.Center()),
                                            Item = o
                                        })
                                    .Where(
                                        o => o.Bbox_x <= image.Width
                                             && o.Bbox_y <= image.Height
                                             && o.Overlap >= min_score_for_linebbox_overlap
                                        )
                                    .OrderBy(o => o.Distance);*/

                var overlapItems = from item in yoloItems
                                   let o = new
                                   {
                                       Overlap = Utils.Utils.CheckLineBboxOverlapRatio(_lines[lineID].coordinates, item.X, item.Y, item.Width, item.Height),
                                       Bbox_x = item.X + item.Width,
                                       Bbox_y = item.Y + item.Height,
                                       Distance = Distance(_lines[lineID].coordinates, item.Center()),
                                       Item = item
                                   }
                                   //where o.Overlap>=min_score_for_linebbox_overlap && o.Bbox_x<=image.Width && o.Bbox_y<=image.Height
                                   orderby o.Distance
                                   select o;
                var ovItems = overlapItems.ToList();
                foreach (var item in overlapItems)
                {
                    if (item.Overlap >= min_score_for_linebbox_overlap)
                    {
                        if (item.Bbox_x <= image.Width && item.Bbox_y <= image.Height)
                        {
                            var taggedImageData = Utils.Utils.DrawImage(imgByte, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height, bboxColor);
                            var croppedImageData = Utils.Utils.CropImage(imgByte, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height);
                            validObjects.Add(new YoloTrackingItem(item.Item, _frameYoloTracking._index, taggedImageData, croppedImageData));
                            _frameYoloTracking._index++;
                        }
                    }
                }
                return (validObjects.Count == 0 ? null : validObjects);
            }
            else
            {
                return yoloItems.ToList();
            }
        }

        private List<IItemID> OverlapVal(byte[] imgByte, IEnumerable<IItemID> yoloItems, int lineID, double min_score_for_linebbox_overlap)
        {
            var image = Image.FromStream(new MemoryStream(imgByte)); // to filter out bbox larger than the frame

            if (yoloItems == null) return null;

            //run overlap ratio-based validation
            if (_lines != null)
            {
                List<IItemID> validObjects = new List<IItemID>();

                ////--------------Output images with all bboxes----------------
                //byte[] canvas = new byte[imgByte.Length];
                //canvas = imgByte;
                //YoloTrackingItem[] dbItems = yoloItems.ToArray();
                //double dbOverlap;
                //foreach (var dbItem in dbItems)
                //{
                //    dbOverlap = Utils.Utils.checkLineBboxOverlapRatio(lines[lineID].Item2, dbItem.X, dbItem.Y, dbItem.Width, dbItem.Height);
                //    canvas = Utils.Utils.DrawImage(canvas, dbItem.X, dbItem.Y, dbItem.Width, dbItem.Height, bboxColor, dbOverlap.ToString());
                //}
                //File.WriteAllBytes(@OutputFolder.OutputFolderAll + $"tmp-{frameIndex}-{lines[lineID].Item1}.jpg", canvas);
                ////--------------Output images with all bboxes----------------

                /*var overlapItems = yoloItems
                                    .Select(
                                        o => new
                                        {
                                            Overlap = Utils.Utils.CheckLineBboxOverlapRatio(_lines[lineID].coordinates, o.X, o.Y, o.Width, o.Height),
                                            Bbox_x = o.X + o.Width,
                                            Bbox_y = o.Y + o.Height,
                                            Distance = this.Distance(_lines[lineID].coordinates, o.Center()),
                                            Item = o
                                        })
                                    .Where(
                                        o => o.Bbox_x <= image.Width
                                             && o.Bbox_y <= image.Height
                                             && o.Overlap >= min_score_for_linebbox_overlap
                                        )
                                    .OrderBy(o => o.Distance);*/

                var overlapItems = from item in yoloItems
                                   let o = new
                                   {
                                       Overlap = Utils.Utils.CheckLineBboxOverlapRatio(_lines[lineID].coordinates, item.BoundingBox),
                                       Bbox_x = item.BoundingBox.Right,
                                       Bbox_y = item.BoundingBox.Bottom,
                                       Distance = Distance(_lines[lineID].coordinates, item.BoundingBox.Center()),
                                       Item = item
                                   }
                                   //where o.Overlap>=min_score_for_linebbox_overlap && o.Bbox_x<=image.Width && o.Bbox_y<=image.Height
                                   orderby o.Distance
                                   select o;
                var ovItems = overlapItems.ToList();
                foreach (var item in overlapItems)
                {
                    if (item.Overlap >= min_score_for_linebbox_overlap)
                    {
                        if (item.Bbox_x <= image.Width && item.Bbox_y <= image.Height)
                        {
                            /*var taggedImageData = Utils.Utils.DrawImage(imgByte, item.Item.BoundingBox.X, item.Item.Y, item.Item.Width, item.Item.Height, bboxColor);
                            var croppedImageData = Utils.Utils.CropImage(imgByte, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height);*/
                            validObjects.Add(item.Item);
                            _frameYoloTracking._index++;
                        }
                    }
                }
                return (validObjects.Count == 0 ? null : validObjects);
            }
            else
            {
                return yoloItems.ToList();
            }
        }
        public static void BuildImageData(YoloTrackingItem yoloItem, Color color, Mat imageData)
        {
            int bbx = yoloItem.X + yoloItem.Width;
            int bby = yoloItem.Y + yoloItem.Height;
            yoloItem.TaggedImageData = Utils.Utils.DrawImage(imageData, yoloItem.X, yoloItem.Y, yoloItem.Width, yoloItem.Height, color);
            yoloItem.CroppedImageData = Utils.Utils.CropImage(imageData, yoloItem.X, yoloItem.Y, yoloItem.Width, yoloItem.Height);
        }
        private static void BuildImageData(YoloTrackingItem yoloItem, Color color, byte[] imageData)
        {
            int bbx = yoloItem.X + yoloItem.Width;
            int bby = yoloItem.Y + yoloItem.Height;
            yoloItem.TaggedImageData = Utils.Utils.DrawImage(imageData, yoloItem.X, yoloItem.Y, yoloItem.Width, yoloItem.Height, color);
            yoloItem.CroppedImageData = Utils.Utils.CropImage(imageData, yoloItem.X, yoloItem.Y, yoloItem.Width, yoloItem.Height);
        }
        private List<YoloTrackingItem> OverlapVal(byte[] imgByte, IEnumerable<YoloTrackingItem> yoloItems, LineSegment line, Color bboxColor, double min_score_for_linebbox_overlap)
        {
            var image = Image.FromStream(new MemoryStream(imgByte)); // to filter out bbox larger than the frame

            if (yoloItems == null)
                return null;

            //run overlap ratio-based validation
            if (_lines != null)
            {
                List<YoloTrackingItem> validObjects = new List<YoloTrackingItem>();

                ////--------------Output images with all bboxes----------------
                //byte[] canvas = new byte[imgByte.Length];
                //canvas = imgByte;
                //YoloTrackingItem[] dbItems = yoloItems.ToArray();
                //double dbOverlap;
                //foreach (var dbItem in dbItems)
                //{
                //    dbOverlap = Utils.Utils.checkLineBboxOverlapRatio(lines[lineID].Item2, dbItem.X, dbItem.Y, dbItem.Width, dbItem.Height);
                //    canvas = Utils.Utils.DrawImage(canvas, dbItem.X, dbItem.Y, dbItem.Width, dbItem.Height, bboxColor, dbOverlap.ToString());
                //}
                //File.WriteAllBytes(@OutputFolder.OutputFolderAll + $"tmp-{frameIndex}-{lines[lineID].Item1}.jpg", canvas);
                ////--------------Output images with all bboxes----------------

                var overlapItems = from o in yoloItems
                                   let o2 = new
                                   {
                                       Overlap = Utils.Utils.CheckLineBboxOverlapRatio(line, o.X, o.Y, o.Width, o.Height),
                                       Bbox_x = o.X + o.Width,
                                       Bbox_y = o.Y + o.Height,
                                       Distance = Distance(line, o.Center()),
                                       Item = o
                                   }
                                   where o.X + o.Width <= image.Width
                                   where o.Y + o.Height <= image.Height
                                   where o2.Overlap >= min_score_for_linebbox_overlap
                                   orderby o2.Distance
                                   select o;

                foreach (var item in overlapItems)
                {
                    Utils.Utils.CheckLineBboxOverlapRatio(line, item.X, item.Y, item.Width, item.Height);
                    var taggedImageData = Utils.Utils.DrawImage(imgByte, item.X, item.Y, item.Width, item.Height, bboxColor);
                    var croppedImageData = Utils.Utils.CropImage(imgByte, item.X, item.Y, item.Width, item.Height);
                    validObjects.Add(new YoloTrackingItem(item, _frameYoloTracking._index, taggedImageData, croppedImageData));
                    _frameYoloTracking._index++;
                }
                return (validObjects.Count == 0 ? null : validObjects);
            }
            else
            {
                return yoloItems.ToList();
            }
        }

        public IList<IFramedItem> Run(Mat imgMat, int frameIndex, List<(string key, LineSegment coordinates)> lines, ISet<string> category, Color bboxColor)
        {
            List<IFramedItem> frameDNNItem = new List<IFramedItem>();

            byte[] imgByte = Utils.Utils.MatToByteBmp(imgMat);

            IEnumerable<YoloTrackingItem> yoloItems = _frameYoloTracking.Analyse(imgByte, category, bboxColor);
            IFrame frame = new Frame("", frameIndex, imgMat);
            for (int lineID = 0; lineID < lines.Count; lineID++)
            {
                //_frameYoloTracking.SetTrackingObject(lines[lineID].coordinates.MidPoint);
                List<YoloTrackingItem> analyzedTrackingItems = OverlapVal(imgByte, yoloItems, lineID, bboxColor, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE);
                if (analyzedTrackingItems == null) continue;

                foreach (YoloTrackingItem yoloTrackingItem in analyzedTrackingItems)
                {
                    Rectangle bounds = new Rectangle(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height);
                    LineTriggeredItemID item = new LineTriggeredItemID(bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.TrackId, nameof(YoloTracking))
                    {
                        TriggerLineID = lineID,
                        TriggerLine = lines[lineID].key
                    };
                    frameDNNItem.Add(new FramedItem(frame, item));

                    //--------------output frameDNN results - one bbox per image--------------
                    string blobName_FrameDNN = $@"frame-{frameIndex}-FrameDNN-{yoloTrackingItem.Confidence}.jpg";
                    string fileName_FrameDNN = @OutputFolder.OutputFolderFrameDNNDarknet + blobName_FrameDNN;
                    File.WriteAllBytes(fileName_FrameDNN, yoloTrackingItem.TaggedImageData);
                    //File.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_FrameDNN, yoloTrackingItem.TaggedImageData);
                    //--------------output frameDNN results - one bbox per image--------------
                }
                DrawAllBb(frameIndex, imgByte, analyzedTrackingItems, bboxColor);
            }
            return frameDNNItem;
        }
        public IList<IFramedItem> Run(IFrame frame, IDictionary<string, LineSegment> lines, ISet<string> category, Color bboxColor)
        {
            List<IFramedItem> frameDNNItem = new List<IFramedItem>();

            byte[] imgByte = Utils.Utils.MatToByteBmp(frame.FrameData);

            IEnumerable<YoloTrackingItem> yoloItems = _frameYoloTracking.Analyse(imgByte, category, bboxColor);
            //IFrame frame = new Frame("", frameIndex, imgMat);
            foreach (var entry in lines)
            {
                //_frameYoloTracking.SetTrackingObject(entry.Value.MidPoint);
                List<YoloTrackingItem> analyzedTrackingItems = OverlapVal(imgByte, yoloItems, 0, bboxColor, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE);
                if (analyzedTrackingItems == null)
                    continue;

                foreach (YoloTrackingItem yoloTrackingItem in analyzedTrackingItems)
                {
                    Rectangle bounds = new Rectangle(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height);
                    LineTriggeredItemID item = new LineTriggeredItemID(bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.TrackId, nameof(YoloTracking))
                    {
                        //item.TriggerLineID = lineID;
                        TriggerLine = entry.Key
                    };
                    frameDNNItem.Add(new FramedItem(frame, item));

                    //--------------output frameDNN results - one bbox per image--------------
                    string blobName_FrameDNN = $@"frame-{frame.FrameIndex}-FrameDNN-{yoloTrackingItem.Confidence}.jpg";
                    string fileName_FrameDNN = @OutputFolder.OutputFolderFrameDNNDarknet + blobName_FrameDNN;
                    File.WriteAllBytes(fileName_FrameDNN, yoloTrackingItem.TaggedImageData);
                    //File.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_FrameDNN, yoloTrackingItem.TaggedImageData);
                    //--------------output frameDNN results - one bbox per image--------------
                }
                DrawAllBb(frame.FrameIndex, imgByte, analyzedTrackingItems, bboxColor);
            }
            return frameDNNItem;
        }

        private static double Distance(LineSegment line, Point bboxCenter)
        {
            Point p1 = line.MidPoint;
            return Math.Sqrt(Pow2(bboxCenter.X - p1.X) + Pow2(bboxCenter.Y - p1.Y));
        }
        private static double Distance(LineSegment line, PointF bboxCenter)
        {
            Point p1 = line.MidPoint;
            return Math.Sqrt(Pow2(bboxCenter.X - p1.X) + Pow2(bboxCenter.Y - p1.Y));
        }

        private static double Pow2(double x)
        {
            return x * x;
        }


        private static void DrawAllBb(int frameIndex, byte[] imgByte, List<YoloTrackingItem> items, Color bboxColor)
        {
            byte[] canvas = imgByte;
            foreach (var item in items)
            {
                canvas = Utils.Utils.DrawImage(canvas, item.X, item.Y, item.Width, item.Height, bboxColor);
            }
            File.WriteAllBytes(@OutputFolder.OutputFolderAll + $"frame-{frameIndex}.jpg", canvas);
        }

        private static Item Item(YoloTrackingItem yoloTrackingItem)
        {
            Item item = new Item(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height,
                yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, 0, "")
            {
                TrackID = yoloTrackingItem.TrackId,
                Index = yoloTrackingItem.Index,
                TaggedImageData = yoloTrackingItem.TaggedImageData,
                CroppedImageData = yoloTrackingItem.CroppedImageData
            };

            return item;
        }
    }
}
