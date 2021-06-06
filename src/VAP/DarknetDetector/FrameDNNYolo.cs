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
using Wrapper.Yolo;
using Wrapper.Yolo.Model;
using Point = System.Drawing.Point;
namespace DarknetDetector
{
    public class FrameDNNDarknet
    {
        private YoloWrapper frameYolo;
        private YoloTracking frameYoloTracking;
        private List<(string key, LineSegment coordinates)> _lines;

        //distance-based validation
        public FrameDNNDarknet(string modelConfig, DNNMode dnnMode, double rFactor)
        {
            var configurationDetector = new ConfigurationDetector(modelConfig);
            frameYolo = new YoloWrapper(configurationDetector.Detect(), dnnMode);
            frameYoloTracking = new YoloTracking(frameYolo, Convert.ToInt32(DNNConfig.ValidRange * rFactor));
        }

        //overlap ratio-based validation
        public FrameDNNDarknet(string modelConfig, DNNMode dnnMode, List<(string key, LineSegment coordinates)> lines)
        {
            var configurationDetector = new ConfigurationDetector(modelConfig);
            frameYolo = new YoloWrapper(configurationDetector.Detect(), dnnMode);
            frameYoloTracking = new YoloTracking(frameYolo);
            _lines = lines;
        }

        //without validation i.e., output all yolo detections
        public FrameDNNDarknet(string modelConfig, DNNMode dnnMode)
        {
            var configurationDetector = new ConfigurationDetector(modelConfig);
            frameYolo = new YoloWrapper(configurationDetector.Detect(), dnnMode);
            frameYoloTracking = new YoloTracking(frameYolo);
        }

        public void SetTrackingPoint(Point trackingObject)
        {
            frameYoloTracking.SetTrackingObject(trackingObject);
        }

        public List<YoloTrackingItem> Detect(Mat imgByte, HashSet<string> category, int lineID, Color bboxColor, double min_score_for_linebbox_overlap, int frameIndex = 0)
        {
            byte[] imgBuffer = imgByte.ToBytes(".bmp");
            IEnumerable<YoloTrackingItem> yoloItems = frameYoloTracking.Analyse(imgBuffer, category, bboxColor);
            
            return OverlapVal( imgBuffer, yoloItems, lineID, bboxColor, min_score_for_linebbox_overlap);
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
                
                var overlapItems = yoloItems.Select(o => new { Overlap = Utils.Utils.checkLineBboxOverlapRatio(_lines[lineID].coordinates, o.X, o.Y, o.Width, o.Height), Bbox_x = o.X + o.Width, Bbox_y = o.Y + o.Height, Distance = this.Distance(_lines[lineID].coordinates, o.Center()), Item = o })
                    .Where(o => o.Bbox_x <= image.Width && o.Bbox_y <= image.Height && o.Overlap >= min_score_for_linebbox_overlap).OrderBy(o => o.Distance);
                foreach (var item in overlapItems)
                {
                    var taggedImageData = Utils.Utils.DrawImage(imgByte, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height, bboxColor);
                    var croppedImageData = Utils.Utils.CropImage(imgByte, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height);
                    validObjects.Add(new YoloTrackingItem(item.Item, frameYoloTracking._index, taggedImageData, croppedImageData));
                    frameYoloTracking._index++;
                }
                return (validObjects.Count == 0 ? null : validObjects);
            }
            else
            {
                return yoloItems.ToList();
            }
        }

        public IList<IFramedItem> Run(Mat imgMat, int frameIndex, List<(string key, LineSegment coordinates)> lines, HashSet<string> category, Color bboxColor )
        {
            List<IFramedItem> frameDNNItem = new List<IFramedItem>();

            byte[] imgByte = Utils.Utils.MatToByteBmp(imgMat);

            IEnumerable<YoloTrackingItem> yoloItems = frameYoloTracking.Analyse(imgByte, category, bboxColor);
            IFrame frame = new Frame("", frameIndex, imgMat);
            for (int lineID = 0; lineID < lines.Count; lineID++)
            {
                frameYoloTracking.SetTrackingObject( lines[lineID].coordinates.MidPoint );
                List<YoloTrackingItem> analyzedTrackingItems = OverlapVal(imgByte, yoloItems, lineID, bboxColor, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE);
                if (analyzedTrackingItems == null) continue;

                foreach ( YoloTrackingItem yoloTrackingItem in analyzedTrackingItems )
                {
                    Rectangle bounds = new Rectangle( yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height );
                    LineTriggeredItemID item = new LineTriggeredItemID(bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.TrackId, nameof(YoloTracking) );

                    item.TriggerLineID = lineID;
                    item.TriggerLine = lines[lineID].key;
                    frameDNNItem.Add( new FramedItem( frame, item ) );

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

        private double Distance(LineSegment line, Point bboxCenter)
        {
            Point p1 = line.MidPoint;
            return Math.Sqrt(this.Pow2(bboxCenter.X - p1.X) + Pow2(bboxCenter.Y - p1.Y));
        }

        private double Pow2(double x)
        {
            return x * x;
        }


        private void DrawAllBb(int frameIndex, byte[] imgByte, List<YoloTrackingItem> items, Color bboxColor)
        {
            byte[] canvas = new byte[imgByte.Length];
            canvas = imgByte;
            foreach (var item in items)
            {
                canvas = Utils.Utils.DrawImage(canvas, item.X, item.Y, item.Width, item.Height, bboxColor);
            }
            File.WriteAllBytes(@OutputFolder.OutputFolderAll + $"frame-{frameIndex}.jpg", canvas);
        }

        private Item Item(YoloTrackingItem yoloTrackingItem)
        {
            Item item = new Item(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height,
                yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, 0, "");

            item.TrackID = yoloTrackingItem.TrackId;
            item.Index = yoloTrackingItem.Index;
            item.TaggedImageData = yoloTrackingItem.TaggedImageData;
            item.CroppedImageData = yoloTrackingItem.CroppedImageData;

            return item;
        }
    }
}
