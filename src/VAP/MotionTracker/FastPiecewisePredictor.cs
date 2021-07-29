// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;

namespace MotionTracker
{
    [DataContract]
    public class FastPiecewisePredictor : IPathPredictor
    {
        public FastPiecewisePredictor()
            : this(20)
        {}

        public FastPiecewisePredictor(int segmentLength)
        {
            SegmentLength = segmentLength;
        }

        public bool CanPredict(IItemPath path, int frameIndex)
        {
            int nearestIndex = path.IndexOfNearestFrame(frameIndex);
            var nearestFrame = path.FramedItems[nearestIndex];
            int dist = Math.Abs(nearestFrame.Frame.FrameIndex - frameIndex);
            return (path.FramedItems.Count >= SegmentLength && dist < SegmentLength * 9)
                || (path.FramedItems.Count >= 9 && dist < path.FramedItems.Count * 6)
                || (path.FramedItems.Count >= 4 && dist < path.FramedItems.Count * 3);
        }

        [DataMember]
        public int SegmentLength { get; set; }

        public Rectangle Predict(IItemPath path, int frameIndex)
        {
            if (path.FramedItems.Count < 9)
            {
                (var a, var b) = path.GetCenterLineFunc();

                int width = 0;
                int height = 0;

                for (int i = 0; i < path.FramedItems.Count; i++)
                {
                    var f = path.FramedItems[i];
                    var box = f.ItemIDs[f.HighestConfidenceIndex].BoundingBox;
                    width += box.Width;
                    height += box.Height;
                }
                float w = width * 1.0f / path.FramedItems.Count;
                float h = height * 1.0f / path.FramedItems.Count;

                var center = a * frameIndex + b;
                return new Rectangle((int)(0.5f + center.X - w / 2), (int)(0.5f + center.Y - h / 2), (int)(0.5f + w), (int)(0.5f + h));
            }
            else if (path.FramedItems.Count < SegmentLength)
            {
                (var a, var b, var c) = path.GetCenterQuadratic();
                int width = 0;
                int height = 0;

                for (int i = 0; i < path.FramedItems.Count; i++)
                {
                    var f = path.FramedItems[i];
                    var box = f.ItemIDs[f.HighestConfidenceIndex].BoundingBox;
                    width += box.Width;
                    height += box.Height;
                }
                float w = width * 1.0f / path.FramedItems.Count;
                float h = height * 1.0f / path.FramedItems.Count;

                var center = a * (frameIndex * frameIndex) + b * frameIndex + c;

                return new Rectangle((int)(0.5f + center.X - w / 2), (int)(0.5f + center.Y - h / 2), (int)(0.5f + w), (int)(0.5f + h));
            }
            else
            {
                int itemCount = Math.Max(9, SegmentLength);

                var items = path.NearestItems(frameIndex, itemCount);

                (var a, var b, var c) = Motion.GetCenterQuadratic(items);
                int width = 0;
                int height = 0;

                for (int i = 0; i < path.FramedItems.Count; i++)
                {
                    var f = path.FramedItems[i];
                    var box = f.ItemIDs[f.HighestConfidenceIndex].BoundingBox;
                    width += box.Width;
                    height += box.Height;
                }
                float w = width * 1.0f / path.FramedItems.Count;
                float h = height * 1.0f / path.FramedItems.Count;

                var center = a * (frameIndex * frameIndex) + b * frameIndex + c;

                return new Rectangle((int)(0.5f + center.X - w / 2), (int)(0.5f + center.Y - h / 2), (int)(0.5f + w), (int)(0.5f + h));
            }
        }

        public bool TryPredict(IItemPath path, int frameIndex, out Rectangle? prediction)
        {
            if(CanPredict(path, frameIndex))
            {
                prediction = Predict(path, frameIndex);
                return true;
            }
            prediction = null;
            return false;
        }
    }
}
