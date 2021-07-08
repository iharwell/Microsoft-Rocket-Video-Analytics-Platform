// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Utils.ShapeTools;
using Xunit;

namespace MotionTrackerTest
{
    public class UtilsTest
    {
        LineSegment internalSegment;
        LineSegment externalSegment;
        LineSegment halfSegment;
        LineSegment quarterSegment;
        LineSegment intersectSegment;

        RectangleF bbox;

        Point center;

        LineOverlapFilter filter;

        private LineSegment Rotate90(LineSegment segment, Point center)
        {
            Point p1 = new Point(segment.P1.X - center.X, segment.P1.Y - center.Y);
            Point p2 = new Point(segment.P2.X - center.X, segment.P2.Y - center.Y);

            p1 = new Point(-p1.Y+center.X, p1.X + center.Y);
            p2 = new Point(-p2.Y + center.X, p2.X + center.Y);
            return new LineSegment(p1, p2);
        }

        public UtilsTest()
        {
            bbox = new RectangleF(100, 100, 100, 100);
            internalSegment = new LineSegment(new Point(130, 130), new Point(170, 170));
            externalSegment = new LineSegment(new Point(30, 30), new Point(170, 170));
            halfSegment = new LineSegment(new Point(70, 150), new Point(130, 150));
            quarterSegment = new LineSegment(new Point(10, 150), new Point(130, 150));
            intersectSegment = new LineSegment(new Point(50, 150), new Point(250, 150));

            filter = new LineOverlapFilter(null);

            center = new Point(150, 150);
        }

        [Fact]
        public void InternalTest0()
        {
            var testLine = internalSegment;

            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(1.0f, result, 5);
        }
        [Fact]
        public void InternalTest1()
        {
            var testLine = internalSegment;

            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(1.0f, result, 5);
        }
        [Fact]
        public void InternalTest2()
        {
            var testLine = internalSegment;

            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(1.0f, result, 5);
        }
        [Fact]
        public void InternalTest3()
        {
            var testLine = internalSegment;

            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(1.0f, result, 5);
        }
        [Fact]
        public void HalfTest0()
        {
            var testLine = halfSegment;

            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.5f, result, 5);
        }
        [Fact]
        public void HalfTest1()
        {
            var testLine = halfSegment;

            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.5f, result, 5);
        }
        [Fact]
        public void HalfTest2()
        {
            var testLine = halfSegment;

            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.5f, result, 5);
        }
        [Fact]
        public void HalfTest3()
        {
            var testLine = halfSegment;
            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.5f, result, 5);
        }
        [Fact]
        public void QuarterTest0()
        {
            var testLine = quarterSegment;

            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.25f, result, 5);
        }
        [Fact]
        public void QuarterTest1()
        {
            var testLine = quarterSegment;

            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.25f, result, 5);
        }
        [Fact]
        public void QuarterTest2()
        {
            var testLine = quarterSegment;

            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.25f, result, 5);
        }
        [Fact]
        public void QuarterTest3()
        {
            var testLine = quarterSegment;

            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.25f, result, 5);
        }
        [Fact]
        public void ExternalTest0()
        {
            var testLine = externalSegment;
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.0f, result, 5);
        }
        [Fact]
        public void ExternalTest1()
        {
            var testLine = externalSegment;

            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.0f, result, 5);
        }
        [Fact]
        public void ExternalTest2()
        {
            var testLine = externalSegment;

            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.0f, result, 5);
        }
        [Fact]
        public void ExternalTest3()
        {
            var testLine = externalSegment;

            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.0f, result, 5);
        }
        [Fact]
        public void IntersectTest0()
        {
            var testLine = intersectSegment;

            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.5f, result, 5);
        }
        [Fact]
        public void IntersectTest1()
        {
            var testLine = intersectSegment;

            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.5f, result, 5);
        }
        [Fact]
        public void IntersectTest2()
        {
            var testLine = intersectSegment;

            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.5f, result, 5);
        }
        [Fact]
        public void IntersectTest3()
        {
            var testLine = intersectSegment;

            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            testLine = Rotate90(testLine, center);
            var result = Utils.LineOverlapFilter.GetOverlapRatio(testLine, bbox);
            Assert.Equal(0.5f, result, 5);
        }
    }
}
