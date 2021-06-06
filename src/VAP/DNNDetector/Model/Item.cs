// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Drawing;
using Utils;
using Utils.Items;

namespace DNNDetector.Model
{
    public class Item : ILineTriggeredItemID
    {
        public string ObjName { get; set; }
        public double Confidence { get; set; }
        public int X
        {
            get => BoundingBox.X;
            set => boundBox.X = value;
        }
        public int Y
        {
            get => BoundingBox.Y;
            set => boundBox.Y = value;
        }
        public int Width
        {
            get => BoundingBox.Width;
            set => boundBox.Width = value;
        }
        public int Height
        {
            get => BoundingBox.Width;
            set => boundBox.Width = value;
        }
        public int ObjectID { get; set; }
        public int TrackID { get; set; }
        public int Index { get; set; }
        public bool FurtherAnalysisTriggered { get; set; }
        public object SourceObject { get; set; }
        public byte[] RawImageData { get; set; }
        public byte[] TaggedImageData { get; set; }
        public byte[] CroppedImageData { get; set; }
        public string TriggerLine { get; set; }
        public LineSegment TriggerSegment { get; set; }
        public int TriggerLineID { get; set; }
        public string IdentificationMethod { get; set; }
        public Rectangle BoundingBox
        {
            get => boundBox;
            set => boundBox = value;
        }

        private Rectangle boundBox;

        public Item(int x, int y, int width, int height, int catId, string catName, double confidence, int lineID, string lineName)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.ObjectID = catId;
            this.ObjName = catName;
            this.Confidence = confidence;
            this.TriggerLineID = lineID;
            this.TriggerLine = lineName;
        }

        public Item(Wrapper.ORT.ORTItem onnxYoloItem)
        {
            this.X = onnxYoloItem.X;
            this.Y = onnxYoloItem.Y;
            this.Width = onnxYoloItem.Width;
            this.Height = onnxYoloItem.Height;
            this.ObjectID = onnxYoloItem.ObjId;
            this.ObjName = onnxYoloItem.ObjName;
            this.Confidence = onnxYoloItem.Confidence;
            this.TriggerLineID = onnxYoloItem.TriggerLineID;
            this.TriggerLine = onnxYoloItem.TriggerLine;
        }

        public Point Center()
        {
            return new Point(this.X + this.Width / 2, this.Y + this.Height / 2);
        }

        public float[] CenterVec()
        {
            float[] vec = { this.X + this.Width / 2, this.Y + this.Height / 2 };
            return vec;
        }

        public void Print()
        {
            Console.WriteLine("{0} {1,-5} {2} {3,-5} {4} {5,-5} {6} {7,-10} {8} {9,-10:N2}",
                                    "Index:", Index, "ObjID:", ObjectID, "TrackID:", TrackID, "ObjName:", ObjName, "Conf:", Confidence);
        }
    }
}
