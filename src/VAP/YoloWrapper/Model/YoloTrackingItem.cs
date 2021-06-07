// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Wrapper.Yolo.Model
{
    public class YoloTrackingItem : YoloItem
    {
        public YoloTrackingItem(YoloItem yoloItem, int index, byte[] taggedImageData, byte[] croppedImageData)
        {
            this.X = yoloItem.X;
            this.Y = yoloItem.Y;
            this.Width = yoloItem.Width;
            this.Height = yoloItem.Height;
            this.Type = yoloItem.Type;
            this.Confidence = yoloItem.Confidence;
            this.ObjId = yoloItem.ObjId;
            this.TrackId = yoloItem.TrackId;

            this.Index = index;
            this.TaggedImageData = taggedImageData;
            this.CroppedImageData = croppedImageData;
        }

        public int Index { get; set; }
        public byte[] TaggedImageData { get; set; }
        public byte[] CroppedImageData { get; set; }

        public YoloItem GetYoloItem()
        {
            YoloItem item = new YoloItem
            {
                X = this.X,
                Y = this.Y,
                Width = this.Width,
                Height = this.Height,
                Type = this.Type,
                Confidence = this.Confidence,
                ObjId = this.ObjId,
                TrackId = this.TrackId
            };

            return item;
        }
    }
}
