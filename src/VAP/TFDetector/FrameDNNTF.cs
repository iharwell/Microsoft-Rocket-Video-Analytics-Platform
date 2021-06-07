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
        private static int s_imageWidth, s_imageHeight, s_index;
        private static List<(string key, LineSegment coordinates)> s_lines;
        private static HashSet<string> s_category;

        private readonly TFWrapper _tfWrapper = new TFWrapper();
        private byte[] _imageByteArray;
        private readonly Brush _bboxColor = Brushes.Green;

        public FrameDNNTF(List<(string key, LineSegment coordinates)> lines)
        {
            s_lines = lines;
        }

        public IList<IFramedItem> Run(Mat frameTF, int frameIndex, HashSet<string> category, Color bboxColor, double min_score_for_linebbox_overlap, bool saveImg = true)
        {
            s_imageWidth = frameTF.Width;
            s_imageHeight = frameTF.Height;
            s_category = category;
            _imageByteArray = Utils.Utils.ImageToByteJpeg(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameTF));

            IFrame frame = new Frame("", frameIndex, frameTF);

            float[,,] boxes;
            float[,] scores, classes;
            (boxes, scores, classes) = _tfWrapper.Run(_imageByteArray);

            IList<IFramedItem> preValidItems = ValidateItems(boxes, scores, classes, DNNConfig.MIN_SCORE_FOR_TFOBJECT_OUTPUT, frame);
            List<IFramedItem> validObjects = new List<IFramedItem>();

            //run overlap ratio-based validation
            if (s_lines != null)
            {
                for (int lineID = 0; lineID < s_lines.Count; lineID++)
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
                    foreach (var framedItem in preValidItems)
                    {
                        Rectangle rect = framedItem.ItemIDs[0].BoundingBox;
                        float overlap = Utils.Utils.CheckLineBboxOverlapRatio(s_lines[lineID].coordinates, rect);
                        int bbox_x = rect.X + rect.Width;
                        int bbox_y = rect.Y + rect.Height;
                        double dist = Distance(s_lines[lineID].coordinates, rect.Center());
                        if (bbox_y <= s_imageHeight && bbox_x <= s_imageWidth && overlap >= min_score_for_linebbox_overlap)
                        {
                            overlapItems.Add(framedItem);
                        }
                    }

                    overlapItems.Sort((IFramedItem t1, IFramedItem t2) => { return Distance(s_lines[lineID].coordinates, t1.ItemIDs[0].BoundingBox.Center()).CompareTo(Distance(s_lines[lineID].coordinates, t2.ItemIDs[0].BoundingBox.Center())); });

                    foreach (var item in overlapItems)
                    {
                        item.ItemIDs[0].TrackID = s_index;
                        validObjects.Add(item);
                        s_index++;
                    }
                }
            }

            // output tf results
            if (saveImg)
            {
                foreach (IFramedItem it in validObjects)
                {
                    int idIndex = it.ItemIDs.Count - 1;
                    IItemID id = it.ItemIDs[idIndex];

                    string blobName_TF = $@"frame-{frameIndex}-TF-{id.Confidence}.jpg";
                    string fileName_TF = @OutputFolder.OutputFolderFrameDNNTF + blobName_TF;
                    var taggedImage = it.TaggedImageData(idIndex, bboxColor);
                    Utils.Utils.WriteAllBytes(fileName_TF, taggedImage);
                    Utils.Utils.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_TF, taggedImage);
                    Utils.Utils.WriteAllBytes(@OutputFolder.OutputFolderFrameDNNTF + $"frame-{frameIndex}-TF-{id.Confidence}.jpg", taggedImage);
                    Utils.Utils.WriteAllBytes(@OutputFolder.OutputFolderAll + $"frame-{frameIndex}-TF-{id.Confidence}.jpg", taggedImage);
                }
            }

            return (validObjects.Count == 0 ? null : validObjects);
        }

        private IList<IFramedItem> ValidateItems(float[,,] boxes, float[,] scores, float[,] classes, double minScore, IFrame frame)
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
                    CatalogItem catalogItem = TFWrapper.Catalog.FirstOrDefault(item => item.Id == value);
                    if (s_category.Count > 0 && !s_category.Contains(catalogItem.DisplayName)) continue;

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

                    int bbox_x = (int)(xmin * s_imageWidth);
                    int bbox_y = (int)(ymin * s_imageHeight);
                    int bbox_w = (int)((xmax - xmin) * s_imageWidth);
                    int bbox_h = (int)((ymax - ymin) * s_imageHeight);

                    //check line overlap
                    for (int lineID = 0; lineID < s_lines.Count; lineID++)
                    {
                        float ratio = Utils.Utils.CheckLineBboxOverlapRatio(s_lines[lineID].coordinates, bbox_x, bbox_y, bbox_w, bbox_h);
                        if (ratio >= DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL)
                        {
                            Rectangle bounds = new Rectangle(bbox_x, bbox_y, bbox_w, bbox_h);
                            LineTriggeredItemID item = new LineTriggeredItemID(bounds, catalogItem.Id, catalogItem.DisplayName, scores[i, j], lineID, nameof(FrameDNNTF))
                            {
                                TriggerLine = s_lines[lineID].key,
                                TriggerLineID = lineID
                            };
                            IFramedItem framed = new FramedItem(frame, item);

                            frameDNNItem.Add(framed);
                            break;
                        }
                    }
                }
            }

            return frameDNNItem;
        }

        private double Distance(LineSegment line, System.Drawing.Point bboxCenter)
        {
            System.Drawing.Point p1 = line.MidPoint;
            return Math.Sqrt(this.Pow2(bboxCenter.X - p1.X) + Pow2(bboxCenter.Y - p1.Y));
        }
        private double Distance(LineSegment line, System.Drawing.PointF bboxCenter)
        {
            System.Drawing.Point p1 = line.MidPoint;
            return Math.Sqrt(this.Pow2(bboxCenter.X - p1.X) + Pow2(bboxCenter.Y - p1.Y));
        }

        private double Pow2(double x)
        {
            return x * x;
        }
    }
}
