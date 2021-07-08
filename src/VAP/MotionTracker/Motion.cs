// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;
using Utils.ShapeTools;

namespace MotionTracker
{
    public static class Motion
    {

        public static float PathVelocity(IList<IFramedItem> framedItems)
        {
            return PathVelocity(framedItems, 0, framedItems.Count - 1);
        }

        public static float PathVelocity(IItemPath path)
        {
            return PathVelocity(path.FramedItems, 0, path.FramedItems.Count - 1);
        }

        public static float PathVelocity(IItemPath path, int startIndex, int endIndex)
        {
            return PathVelocity(path.FramedItems, startIndex, endIndex);
        }

        /*public static float PathVelocity(IList<IFramedItem> framedItems, int startIndex, int endIndex)
        {
            var startItem = framedItems[startIndex];
            var startCenter = startItem.MeanBounds.Center();
            var distFunc = new Func<IFramedItem, float>((IFramedItem item) =>
            {
                PointF p1 = item.MeanBounds.Center();
                PointF p2 = startCenter;

                float dx = p1.X - p2.X;
                float dy = p1.Y - p2.Y;
                return MathF.Sqrt((dx * dx) + (dy * dy));
            });
            var indexFunc = new Func<IFramedItem, float>((IFramedItem item) =>
            {
                float f1 = item.Frame.FrameIndex;
                float f2 = startItem.Frame.FrameIndex;

                return f1 - f2;
            });

            double[] dists = new double[endIndex - startIndex + 1];
            double[] indices = new double[dists.Length];

            for (int i = 0; i+startIndex < framedItems.Count && i+startIndex <= endIndex; i++)
            {
                var item = framedItems[i+startIndex];
                dists[i] = distFunc(item);
                indices[i] = indexFunc(item);
            }

            var result = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(indices, dists);
            return (float)result.Item2;
        }*/
        public static float PathVelocity(IList<IFramedItem> framedItems, int startIndex, int endIndex)
        {
            var coefs = GetCenterLineFunc(framedItems);
            float xsq = coefs.a.X * coefs.a.X;
            float ysq = coefs.a.Y * coefs.a.Y;
            return MathF.Sqrt(xsq + ysq);
        }

        public static (float iou, IFramedItem match) FindBestMatch(IFramedItem testItem, IList<IFramedItem> searchSet)
        {
            float bestiou = -1.0f;
            IFramedItem bestMatch = null;

            Rectangle testRect = RectangleTools.RoundRectF(testItem.MeanBounds);

            for (int i = 0; i < searchSet.Count; i++)
            {
                Rectangle targetRect = RectangleTools.RoundRectF(searchSet[i].MeanBounds);
                float iou = testRect.IntersectionOverUnion(targetRect);
                if (iou > bestiou)
                {
                    bestiou = iou;
                    bestMatch = searchSet[i];
                }
            }

            return (bestiou, bestMatch);
        }

        public static (Vector2 a, Vector2 b, Vector2 c) GetCenterQuadratic(this IItemPath path)
        {
            int n = path.FramedItems.Count;
            var startPoint = new PointF(0, 0);
            var midPoint = new PointF(0, 0);
            var endPoint = new PointF(0, 0);

            float startFrame = 0;
            float midFrame = 0;
            float endFrame = 0;

            int avgSize = Math.Min(3, n / 3);

            (startPoint, midPoint, endPoint) = GetSmoothedPoints(path, avgSize);
            (startFrame, midFrame, endFrame) = GetSmoothedFrames(path, avgSize);

            Matrix4x4 matrix = new Matrix4x4(1, 1, 1, 0,
                                             startFrame, midFrame, endFrame, 0,
                                             startFrame * startFrame, midFrame * midFrame, endFrame * endFrame, 0,
                                             0, 0, 0, 1);

            Matrix4x4.Invert(matrix, out var invM);
            matrix = new Matrix4x4(startPoint.X, midPoint.X, endPoint.X, 0,
                                   startPoint.Y, midPoint.Y, endPoint.Y, 0,
                                   0, 0, 0, 0,
                                   0, 0, 0, 0);

            var k = matrix * invM;
            return (new Vector2(k.M13, k.M23), new Vector2(k.M12, k.M22), new Vector2(k.M11, k.M21));
        }
        public static (Vector2 a, Vector2 b) GetCenterLineFunc(this IItemPath path)
        {
            int n = path.FramedItems.Count;
            var startPoint = new PointF(0, 0);
            var midPoint = new PointF(0, 0);
            var endPoint = new PointF(0, 0);

            float startFrame = 0;
            float midFrame = 0;
            float endFrame = 0;

            int avgSize = Math.Max(1, Math.Min(3, n / 3));

            (startPoint, midPoint, endPoint) = GetSmoothedPoints(path, avgSize);
            (startFrame, midFrame, endFrame) = GetSmoothedFrames(path, avgSize);

            Matrix4x4 matrix = new Matrix4x4(1, 1, 0, 0,
                                             startFrame, endFrame, 0, 0,
                                             0, 0, 1, 0,
                                             0, 0, 0, 1);

            Matrix4x4.Invert(matrix, out var invM);
            matrix = new Matrix4x4(startPoint.X, endPoint.X, 0, 0,
                                   startPoint.Y, endPoint.Y, 0, 0,
                                   0, 0, 0, 0,
                                   0, 0, 0, 0);

            var k = matrix * invM;
            return (new Vector2(k.M12, k.M22), new Vector2(k.M11, k.M21));
        }
        public static (Vector2 a, Vector2 b) GetCenterLineFunc(IList<IFramedItem> items)
        {
            int n = items.Count;
            var startPoint = new PointF(0, 0);
            var midPoint = new PointF(0, 0);
            var endPoint = new PointF(0, 0);

            float startFrame = 0;
            float midFrame = 0;
            float endFrame = 0;

            int avgSize = Math.Max(1, Math.Min(3, n / 3));

            (startPoint, midPoint, endPoint) = GetSmoothedPoints(items, avgSize);
            (startFrame, midFrame, endFrame) = GetSmoothedFrames(items, avgSize);

            Matrix4x4 matrix = new Matrix4x4(1, 1, 0, 0,
                                             startFrame, endFrame, 0, 0,
                                             0, 0, 1, 0,
                                             0, 0, 0, 1);

            Matrix4x4.Invert(matrix, out var invM);
            matrix = new Matrix4x4(startPoint.X, endPoint.X, 0, 0,
                                   startPoint.Y, endPoint.Y, 0, 0,
                                   0, 0, 0, 0,
                                   0, 0, 0, 0);

            var k = matrix * invM;
            return (new Vector2(k.M12, k.M22), new Vector2(k.M11, k.M21));
        }

        private static (float startFrame, float midFrame, float endFrame) GetSmoothedFrames(IItemPath path, int averageWindow)
        {
            return GetSmoothedFrames(path.FramedItems, averageWindow);
        }

        private static (float startFrame, float midFrame, float endFrame) GetSmoothedFrames(IList<IFramedItem> items, int averageWindow)
        {
            int n = items.Count;
            var startFrame = 0.0f;
            var midFrame = 0.0f;
            var endFrame = 0.0f;

            int startIndex = 0;
            int midIndex = (n - averageWindow + 1) / 2;
            int endIndex = (n - averageWindow);

            for (int i = 0; i < averageWindow; i++)
            {
                var pt = items[startIndex + i].Frame.FrameIndex;
                startFrame += pt;

                pt = items[midIndex + i].Frame.FrameIndex;
                midFrame += pt;

                pt = items[endIndex + i].Frame.FrameIndex;
                endFrame += pt;
            }

            return (startFrame / averageWindow, midFrame / averageWindow, endFrame / averageWindow);
        }

        private static (PointF startPoint, PointF midPoint, PointF endPoint) GetSmoothedPoints(IItemPath path, int averageWindow)
        {
            return GetSmoothedPoints(path.FramedItems, averageWindow);
        }

        private static (PointF startPoint, PointF midPoint, PointF endPoint) GetSmoothedPoints(IList<IFramedItem> items, int averageWindow)
        {
            int n = items.Count;
            var startPoint = new PointF(0, 0);
            var midPoint = new PointF(0, 0);
            var endPoint = new PointF(0, 0);

            int startIndex = 0;
            int midIndex = (n - averageWindow) / 2;
            int endIndex = (n - averageWindow);

            for (int i = 0; i < averageWindow; i++)
            {
                var pt = items[startIndex + i].MeanBounds.Center();
                startPoint.X += pt.X;
                startPoint.Y += pt.Y;

                pt = items[midIndex + i].MeanBounds.Center();
                midPoint.X += pt.X;
                midPoint.Y += pt.Y;

                pt = items[endIndex + i].MeanBounds.Center();
                endPoint.X += pt.X;
                endPoint.Y += pt.Y;
            }
            startPoint.X /= averageWindow;
            midPoint.X /= averageWindow;
            endPoint.X /= averageWindow;
            startPoint.Y /= averageWindow;
            midPoint.Y /= averageWindow;
            endPoint.Y /= averageWindow;

            return (startPoint, midPoint, endPoint);
        }
    }
}
