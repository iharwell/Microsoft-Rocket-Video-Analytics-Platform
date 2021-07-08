using System;
using System.Collections.Generic;
using System.Drawing;
using MotionTracker;
using Utils.Items;
using Xunit;

namespace MotionTrackerTest
{
    public class MotionTest
    {
        ItemPath path1to11;
        ItemPath path6to11to18;

        public MotionTest()
        {
            {
                path1to11 = new ItemPath();
                ItemID id1 = new ItemID()
                {
                    BoundingBox = CenteredRectangle(1, 1)
                };
                ItemID id11 = new ItemID()
                {
                    BoundingBox = CenteredRectangle(11, 11)
                };
                Frame f1 = new Frame()
                {
                    FrameIndex = 1
                };
                Frame f11 = new Frame()
                {
                    FrameIndex = 11
                };

                FramedItem fi1 = new FramedItem(f1, id1);
                FramedItem fi11 = new FramedItem(f11, id11);

                path1to11.FramedItems.Add(fi1);
                path1to11.FramedItems.Add(fi11);
            }

            {
                path6to11to18 = new ItemPath();
                ItemID id1 = new ItemID()
                {
                    BoundingBox = CenteredRectangle(6, -6)
                };
                ItemID id2 = new ItemID()
                {
                    BoundingBox = CenteredRectangle(11, -11)
                };
                ItemID id3 = new ItemID()
                {
                    BoundingBox = CenteredRectangle(18, -18)
                };
                Frame f1 = new Frame()
                {
                    FrameIndex = 1
                };
                Frame f2 = new Frame()
                {
                    FrameIndex = 2
                };
                Frame f3 = new Frame()
                {
                    FrameIndex = 3
                };

                FramedItem fi1 = new FramedItem(f1, id1);
                FramedItem fi2 = new FramedItem(f2, id2);
                FramedItem fi3 = new FramedItem(f3, id3);

                path6to11to18.FramedItems.Add(fi1);
                path6to11to18.FramedItems.Add(fi2);
                path6to11to18.FramedItems.Add(fi3);
            }

        }

        private Rectangle CenteredRectangle(int centerX, int centerY)
        {
            return new Rectangle(centerX - 1, centerY - 1, 2, 2);
        }

        [Fact]
        public void CenterLineTestX()
        {
            ItemPath path = new ItemPath();
            ItemID id1 = new ItemID()
            {
                BoundingBox = CenteredRectangle(1, 1)
            };
            ItemID id11 = new ItemID()
            {
                BoundingBox = CenteredRectangle(11, 11)
            };
            Frame f1 = new Frame()
            {
                FrameIndex = 1
            };
            Frame f11 = new Frame()
            {
                FrameIndex = 11
            };

            FramedItem fi1 = new FramedItem(f1, id1);
            FramedItem fi11 = new FramedItem(f11, id11);

            path.FramedItems.Add(fi1);
            path.FramedItems.Add(fi11);

            var result = path.GetCenterLineFunc();

            Xunit.Assert.Equal(1.0f, result.a.X, 5);
        }

        [Fact]
        public void CenterLineTestY()
        {
            ItemPath path = new ItemPath();
            ItemID id1 = new ItemID()
            {
                BoundingBox = CenteredRectangle(1, 1)
            };
            ItemID id11 = new ItemID()
            {
                BoundingBox = CenteredRectangle(11, 11)
            };
            Frame f1 = new Frame()
            {
                FrameIndex = 1
            };
            Frame f11 = new Frame()
            {
                FrameIndex = 11
            };

            FramedItem fi1 = new FramedItem(f1, id1);
            FramedItem fi11 = new FramedItem(f11, id11);

            path.FramedItems.Add(fi1);
            path.FramedItems.Add(fi11);

            var result = path.GetCenterLineFunc();

            Xunit.Assert.Equal(1.0f, result.a.Y, 5);
        }


        [Fact]
        public void CenterLineQuadTestX()
        {
            ItemPath path = new ItemPath();
            ItemID id1 = new ItemID()
            {
                BoundingBox = CenteredRectangle(6, -6)
            };
            ItemID id2 = new ItemID()
            {
                BoundingBox = CenteredRectangle(11, -11)
            };
            ItemID id3 = new ItemID()
            {
                BoundingBox = CenteredRectangle(18, -18)
            };
            Frame f1 = new Frame()
            {
                FrameIndex = 1
            };
            Frame f2 = new Frame()
            {
                FrameIndex = 2
            };
            Frame f3 = new Frame()
            {
                FrameIndex = 3
            };

            FramedItem fi1 = new FramedItem(f1, id1);
            FramedItem fi2 = new FramedItem(f2, id2);
            FramedItem fi3 = new FramedItem(f3, id3);

            path.FramedItems.Add(fi1);
            path.FramedItems.Add(fi2);
            path.FramedItems.Add(fi3);

            var result = path.GetCenterQuadratic();

            Xunit.Assert.Equal(1.0f, result.a.X, 5);
            Xunit.Assert.Equal(2.0f, result.b.X, 5);
            Xunit.Assert.Equal(3.0f, result.c.X, 5);
        }

        [Fact]
        public void CenterLineQuadTestY()
        {
            ItemPath path = new ItemPath();
            ItemID id1 = new ItemID()
            {
                BoundingBox = CenteredRectangle(6, -6)
            };
            ItemID id2 = new ItemID()
            {
                BoundingBox = CenteredRectangle(11, -11)
            };
            ItemID id3 = new ItemID()
            {
                BoundingBox = CenteredRectangle(18, -18)
            };
            Frame f1 = new Frame()
            {
                FrameIndex = 1
            };
            Frame f2 = new Frame()
            {
                FrameIndex = 2
            };
            Frame f3 = new Frame()
            {
                FrameIndex = 3
            };

            FramedItem fi1 = new FramedItem(f1, id1);
            FramedItem fi2 = new FramedItem(f2, id2);
            FramedItem fi3 = new FramedItem(f3, id3);

            path.FramedItems.Add(fi1);
            path.FramedItems.Add(fi2);
            path.FramedItems.Add(fi3);

            var result = path.GetCenterQuadratic();

            Xunit.Assert.Equal(-1.0f, result.a.Y, 5);
            Xunit.Assert.Equal(-2.0f, result.b.Y, 5);
            Xunit.Assert.Equal(-3.0f, result.c.Y, 5);
        }

        [Fact]
        public void VelocityTest1()
        {
            Assert.Equal(Math.Sqrt(2), Motion.PathVelocity(path1to11), 6);
        }

        [Fact]
        public void MeanBoundsTest()
        {
            List<IItemID> ids = new()
            {
                new ItemID(new Rectangle(1, 2, 3, 4), 0, null, 0, 0, null),
                new ItemID(new Rectangle(6, 7, 8, 9), 0, null, 0, 0, null),
            };

            var result = Utils.ShapeTools.StatisticRectangle.MeanBox(ids);

            Assert.Equal(3.5f, result.X, 4);
            Assert.Equal(4.5f, result.Y, 4);
            Assert.Equal(5.5f, result.Width, 4);
            Assert.Equal(6.5f, result.Height, 4);
        }
    }
}
