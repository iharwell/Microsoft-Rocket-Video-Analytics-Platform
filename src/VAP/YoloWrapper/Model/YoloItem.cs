// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Drawing;

namespace Wrapper.Yolo.Model
{
    public class YoloItem
    {
        public YoloItem() { }
        public YoloItem(Rectangle bbox, string type, double confidence)
        {
            BBox = bbox;
            Type = type;
            Confidence = confidence;
        }
        public YoloItem(Rectangle bbox, string type, double confidence, int objId, int trackId)
        {
            BBox = bbox;
            Type = type;
            Confidence = confidence;
            ObjId = objId;
            TrackId = trackId;
        }
        public YoloItem(Rectangle bbox, double confidence, int objId, int trackId)
            : this( bbox, null, confidence, objId, trackId )
        { }

        public string Type { get; set; }
        public double Confidence { get; set; }
        public int X
        {
            get => _bbox.X;
            set => _bbox.X = value;
        }
        public int Y
        {
            get => _bbox.Y;
            set => _bbox.Y = value;
        }
        public int Width
        {
            get => _bbox.Width;
            set => _bbox.Width = value;
        }
        public int Height
        {
            get => _bbox.Height;
            set => _bbox.Height = value;
        }
        public int ObjId { get; set; }
        public int TrackId { get; set; }

        public Rectangle BBox
        {
            get => _bbox;
            set => _bbox = value;
        }

        private Rectangle _bbox;

        public Point Center()
        {
            return new Point(this.X + this.Width / 2, this.Y + this.Height / 2);
        }

        public float[] CenterVec()
        {
            float[] vec = { this.X + this.Width / 2, this.Y + this.Height / 2 };
            return vec;
        }
    }
}
