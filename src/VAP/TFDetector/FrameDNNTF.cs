// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Config;
using DNNDetector.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Utils;
using Utils.Config;
using Utils.Items;
using Utils.ShapeTools;
using Wrapper.TF;
using Wrapper.TF.Common;

namespace TFDetector
{
    public class FrameDNNTF
    {
        private static int _imageWidth, _imageHeight, _index;
        private static List<(string key, LineSegment coordinates)> _lines;
        private static HashSet<string> _category;

        TFWrapper tfWrapper = new TFWrapper();
        byte[] imageByteArray;
        Brush bboxColor = Brushes.Green;

        public FrameDNNTF(List<(string key, LineSegment coordinates)> lines)
        {
            _lines = lines;
        }

        public IList<IFramedItem> Run(Mat frameTF, int frameIndex, HashSet<string> category, Brush bboxColor, double min_score_for_linebbox_overlap, bool saveImg = true)
        {
            _imageWidth = frameTF.Width;
            _imageHeight = frameTF.Height;
            _category = category;
            imageByteArray = Utils.Utils.ImageToByteJpeg(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameTF));

            IFrame frame = new Frame( "", frameIndex, imageByteArray );

            float[,,] boxes;
            float[,] scores, classes;
            (boxes, scores, classes) = tfWrapper.Run(imageByteArray);
            
            IList<IFramedItem> preValidItems = ValidateItems(boxes, scores, classes, DNNConfig.MIN_SCORE_FOR_TFOBJECT_OUTPUT, frame);
            List<IFramedItem> validObjects = new List<IFramedItem>();

            //run overlap ratio-based validation
            if (_lines != null)
            {
                for (int lineID = 0; lineID < _lines.Count; lineID++)
                {
                    /*var overlapItems = preValidItems
                        .Select(
                            o => new 
                            {
                                Overlap = Utils.Utils.checkLineBboxOverlapRatio(_lines[lineID].coordinates, o.ItemIDs[0].BoundingBox.X, o.ItemIDs[0].BoundingBox.Y, o.ItemIDs[0].BoundingBox.Width, o.ItemIDs[0].BoundingBox.Height),
                                Bbox_x = o.ItemIDs[0].BoundingBox.X + o.ItemIDs[0].BoundingBox.Width,
                                Bbox_y = o.ItemIDs[0].BoundingBox.Y + o.ItemIDs[0].BoundingBox.Height,
                                Distance = this.Distance(_lines[lineID].coordinates, o.ItemIDs[0].BoundingBox.Center()), Item = o
                            })
                        .Where(
                                o =>
                                o.Bbox_x <= _imageWidth
                                && o.Bbox_y <= _imageHeight
                                && o.Overlap >= min_score_for_linebbox_overlap
                            )
                        .OrderBy(o => o.Distance);*/
                    List<IFramedItem> overlapItems = new List<IFramedItem>();
                    foreach ( var framedItem in preValidItems )
                    {
                        Rectangle rect = framedItem.ItemIDs[0].BoundingBox;
                        float overlap = Utils.Utils.checkLineBboxOverlapRatio( _lines[lineID].coordinates, rect );
                        int bbox_x = rect.X + rect.Width;
                        int bbox_y = rect.Y + rect.Height;
                        double dist = Distance( _lines[lineID].coordinates, rect.Center() );
                        if( bbox_y <= _imageHeight && bbox_x <= _imageWidth && overlap>= min_score_for_linebbox_overlap )
                        {
                            overlapItems.Add( framedItem );
                        }
                    }

                    overlapItems.Sort( ( IFramedItem t1, IFramedItem t2 ) => { return Distance( _lines[lineID].coordinates, t1.ItemIDs[0].BoundingBox.Center() ).CompareTo( Distance( _lines[lineID].coordinates, t2.ItemIDs[0].BoundingBox.Center() ) ); } );

                    foreach (var item in overlapItems)
                    {
                        item.ItemIDs[0].TrackID = _index;
                        validObjects.Add(item);
                        _index++;
                    }
                }
            }

            // output tf results
            if (saveImg)
            {
                foreach (Item it in validObjects)
                {
                    string blobName_TF = $@"frame-{frameIndex}-TF-{it.Confidence}.jpg";
                    string fileName_TF = @OutputFolder.OutputFolderFrameDNNTF + blobName_TF;
                    File.WriteAllBytes(fileName_TF, it.TaggedImageData);
                    File.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_TF, it.TaggedImageData);

                    using (Image image = Image.FromStream(new MemoryStream(it.TaggedImageData)))
                    {
                        image.Save(@OutputFolder.OutputFolderFrameDNNTF + $"frame-{frameIndex}-TF-{it.Confidence}.jpg", ImageFormat.Jpeg);
                        image.Save(@OutputFolder.OutputFolderAll + $"frame-{frameIndex}-TF-{it.Confidence}.jpg", ImageFormat.Jpeg);
                    }
                }
            }

            return (validObjects.Count == 0 ? null : validObjects);
        }

        IList<IFramedItem> ValidateItems(float[,,] boxes, float[,] scores, float[,] classes, double minScore, IFrame frame)
        {
            List<IFramedItem> frameDNNItem = new List<IFramedItem>();
            var x = boxes.GetLength(0);
            var y = boxes.GetLength(1);
            var z = boxes.GetLength(2);

            float ymin = 0, xmin = 0, ymax = 0, xmax = 0;

            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    if (scores[i, j] < minScore) continue;

                    int value = Convert.ToInt32(classes[i, j]);
                    CatalogItem catalogItem = TFWrapper._catalog.FirstOrDefault(item => item.Id == value);
                    if (_category.Count > 0 && !_category.Contains(catalogItem.DisplayName)) continue;

                    for (int k = 0; k < z; k++)
                    {
                        var box = boxes[i, j, k];
                        switch (k)
                        {
                            case 0:
                                ymin = box;
                                break;
                            case 1:
                                xmin = box;
                                break;
                            case 2:
                                ymax = box;
                                break;
                            case 3:
                                xmax = box;
                                break;
                        }
                    }

                    int bbox_x = (int)(xmin * _imageWidth);
                    int bbox_y = (int)(ymin * _imageHeight);
                    int bbox_w = (int)((xmax - xmin) * _imageWidth);
                    int bbox_h = (int)((ymax - ymin) * _imageHeight);
                    
                    //check line overlap
                    for (int lineID = 0; lineID < _lines.Count; lineID++)
                    {
                        float ratio = Utils.Utils.checkLineBboxOverlapRatio(_lines[lineID].coordinates, bbox_x, bbox_y, bbox_w, bbox_h);
                        if (ratio >= DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL)
                        {
                            Rectangle bounds = new Rectangle(bbox_x, bbox_y, bbox_w, bbox_h);
                            LineTriggeredItemID item = new LineTriggeredItemID(bounds, catalogItem.Id, catalogItem.DisplayName, scores[i, j], lineID, nameof(FrameDNNTF) );
                            item.TriggerLine = _lines[lineID].key;
                            item.TriggerLineID = lineID;
                            IFramedItem framed = new FramedItem( frame, item );

                            frameDNNItem.Add(framed);
                            break;
                        }
                    }
                }
            }

            return frameDNNItem;
        }

        private double Distance( LineSegment line, System.Drawing.Point bboxCenter )
        {
            System.Drawing.Point p1 = line.MidPoint;
            return Math.Sqrt( this.Pow2( bboxCenter.X - p1.X ) + Pow2( bboxCenter.Y - p1.Y ) );
        }
        private double Distance( LineSegment line, System.Drawing.PointF bboxCenter )
        {
            System.Drawing.Point p1 = line.MidPoint;
            return Math.Sqrt( this.Pow2( bboxCenter.X - p1.X ) + Pow2( bboxCenter.Y - p1.Y ) );
        }

        private double Pow2(double x)
        {
            return x * x;
        }
    }
}
