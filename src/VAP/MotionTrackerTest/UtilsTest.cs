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
using X86 = System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Numerics;
using System.Diagnostics;

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

        byte[] bigBytes;
        float[] bigFloats;

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

            bigBytes = new byte[1024*2048*3];
            bigFloats = new float[bigBytes.Length];

            for (int i = 0; i < bigBytes.Length; i++)
            {
                bigBytes[i] = (byte)(i & 0xFF);
            }
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

        [Fact]
        public void ConversionTest()
        {
            byte[] bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };

            float[] expected = new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };

            float[] result = CvtArray3(bytes);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], result[i], 4);
            }
        }

        [Fact]
        public void ConversionTest2()
        {
            for (int i = 0; i < 20; i++)
            {
                CvtArray(bigBytes, bigFloats);
                CvtArray2(bigBytes, bigFloats);
                CvtArray3(bigBytes, bigFloats);

            }

            Stopwatch sw1 = new();
            Stopwatch sw2 = new();
            Stopwatch sw3 = new();

            sw1.Start();
            for (int i = 0; i < 1024; i++)
            {
                CvtArray(bigBytes, bigFloats);
            }
            sw1.Stop();

            var t1 = sw1.ElapsedMilliseconds;
            System.Threading.Thread.Sleep(1000);

            sw3.Start();
            for (int i = 0; i < 1024; i++)
            {
                CvtArray3(bigBytes, bigFloats);
            }
            sw3.Stop();
            System.Threading.Thread.Sleep(1000);

            sw2.Start();
            for (int i = 0; i < 1024; i++)
            {
                CvtArray2(bigBytes, bigFloats);
            }
            sw2.Stop();

            var t2 = sw2.ElapsedMilliseconds;

            var t3 = sw3.ElapsedMilliseconds;

            Console.WriteLine("cvt1: " + t1);
            Console.WriteLine("cvt2: " + t2);
            Console.WriteLine("cvt3: " + t3);
        }

        public static unsafe float[] CvtArray2(byte[] bytes)
        {
            float[] floats = new float[bytes.Length];

            CvtArray2(bytes, floats);
            return floats;
        }

        public static unsafe void CvtArray2(byte[] bytes, float[] floats)
        {
            int i = 0;

            if (X86.Avx2.IsSupported)
            {
                fixed (byte* bptr = bytes)
                fixed (float* fptr = floats)
                {
                    var loadVec = Vector256.Create(0, 8, 16, 24, 4, 12, 20, 28);
                    while (i + 31 < bytes.Length)
                    {
                        //var byteSrc = X86.Avx.LoadVector256(bptr + i);
                        //var byteSrc = X86.Sse2.LoadVector128(bptr + i);
                        var byteSrc = X86.Avx2.GatherVector256((int*)(bptr + i), loadVec, 1).AsByte();
                        var short128A = X86.Avx2.UnpackLow(byteSrc, Vector256.CreateScalar((byte)0)).AsInt16();
                        var short128B = X86.Avx2.UnpackHigh(byteSrc, Vector256.CreateScalar((byte)0)).AsInt16();
                        var int128A = X86.Avx2.UnpackLow(short128A, Vector256.CreateScalar((short)0)).AsInt32();
                        var int128B = X86.Avx2.UnpackHigh(short128A, Vector256.CreateScalar((short)0)).AsInt32();
                        var int128C = X86.Avx2.UnpackLow(short128B, Vector256.CreateScalar((short)0)).AsInt32();
                        var int128D = X86.Avx2.UnpackHigh(short128B, Vector256.CreateScalar((short)0)).AsInt32();
                        var f128A = X86.Avx2.ConvertToVector256Single(int128A);
                        var f128B = X86.Avx2.ConvertToVector256Single(int128B);
                        var f128C = X86.Avx2.ConvertToVector256Single(int128C);
                        var f128D = X86.Avx2.ConvertToVector256Single(int128D);

                        X86.Avx.Store(fptr + i, f128A);
                        X86.Avx.Store(fptr + i + 8, f128B);
                        X86.Avx.Store(fptr + i + 16, f128C);
                        X86.Avx.Store(fptr + i + 24, f128D);
                        i += 32;
                    }
                }
            }
            if (X86.Sse2.IsSupported)
            {
                fixed (byte* bptr = bytes)
                fixed (float* fptr = floats)
                {
                    while (i + 15 < bytes.Length)
                    {
                        var byteSrc = X86.Sse2.LoadVector128(bptr + i);
                        var short128A = X86.Sse2.UnpackLow(byteSrc, Vector128.CreateScalar((byte)0)).AsInt16();
                        var short128B = X86.Sse2.UnpackHigh(byteSrc, Vector128.CreateScalar((byte)0)).AsInt16();
                        var int128A = X86.Sse2.UnpackLow(short128A, Vector128.CreateScalar((short)0)).AsInt32();
                        var int128B = X86.Sse2.UnpackHigh(short128A, Vector128.CreateScalar((short)0)).AsInt32();
                        var int128C = X86.Sse2.UnpackLow(short128B, Vector128.CreateScalar((short)0)).AsInt32();
                        var int128D = X86.Sse2.UnpackHigh(short128B, Vector128.CreateScalar((short)0)).AsInt32();
                        var f128A = X86.Sse2.ConvertToVector128Single(int128A);
                        var f128B = X86.Sse2.ConvertToVector128Single(int128B);
                        var f128C = X86.Sse2.ConvertToVector128Single(int128C);
                        var f128D = X86.Sse2.ConvertToVector128Single(int128D);

                        X86.Sse.Store(fptr + i, f128A);
                        X86.Sse.Store(fptr + i + 4, f128B);
                        X86.Sse.Store(fptr + i + 8, f128C);
                        X86.Sse.Store(fptr + i + 12, f128D);
                        i += 16;
                    }
                }
            }
            while (i < bytes.Length)
            {
                floats[i] = bytes[i];
                ++i;
            }
        }

        public static unsafe float[] CvtArray3(byte[] bytes)
        {
            float[] floats = new float[bytes.Length];

            CvtArray3(bytes, floats);
            return floats;
        }

        public static unsafe void CvtArray3(byte[] bytes, float[] floats)
        {
            int i = 0;
            if (X86.Sse2.IsSupported)
            {
                fixed (byte* bptr = bytes)
                fixed (float* fptr = floats)
                {
                    while (i + 15 < bytes.Length)
                    {
                        var byteSrc = X86.Sse2.LoadVector128(bptr + i);
                        var short128A = X86.Sse2.UnpackLow(byteSrc, Vector128.CreateScalar((byte)0)).AsInt16();
                        var short128B = X86.Sse2.UnpackHigh(byteSrc, Vector128.CreateScalar((byte)0)).AsInt16();
                        var int128A = X86.Sse2.UnpackLow(short128A, Vector128.CreateScalar((short)0)).AsInt32();
                        var int128B = X86.Sse2.UnpackHigh(short128A, Vector128.CreateScalar((short)0)).AsInt32();
                        var int128C = X86.Sse2.UnpackLow(short128B, Vector128.CreateScalar((short)0)).AsInt32();
                        var int128D = X86.Sse2.UnpackHigh(short128B, Vector128.CreateScalar((short)0)).AsInt32();
                        var f128A = X86.Sse2.ConvertToVector128Single(int128A);
                        var f128B = X86.Sse2.ConvertToVector128Single(int128B);
                        var f128C = X86.Sse2.ConvertToVector128Single(int128C);
                        var f128D = X86.Sse2.ConvertToVector128Single(int128D);

                        X86.Sse.Store(fptr + i, f128A);
                        X86.Sse.Store(fptr + i + 4, f128B);
                        X86.Sse.Store(fptr + i + 8, f128C);
                        X86.Sse.Store(fptr + i + 12, f128D);
                        i += 16;
                    }
                }
            }
            while (i < bytes.Length)
            {
                floats[i] = bytes[i];
                ++i;
            }
        }

        public static float[] CvtArray(byte[] bytes)
        {
            float[] floats = new float[bytes.Length];

            CvtArray(bytes, floats);
            return floats;
        }
        public static void CvtArray(byte[] bytes, float[] floats)
        {
            int i = 0;

            /*if (X86.Sse2.IsSupported)
            {
                fixed (byte* bptr = bytes)
                fixed (float* fptr = floats)
                {
                    while (i + 15 < bytes.Length)
                    {
                        var byteSrc = X86.Sse2.LoadVector128(bptr + i);
                        var short128A = X86.Sse2.UnpackLow(byteSrc, Vector128.CreateScalar((byte)0)).AsInt16();
                        var short128B = X86.Sse2.UnpackHigh(byteSrc, Vector128.CreateScalar((byte)0)).AsInt16();
                        var int128A = X86.Sse2.UnpackLow(short128A, Vector128.CreateScalar((short)0)).AsInt32();
                        var int128B = X86.Sse2.UnpackHigh(short128A, Vector128.CreateScalar((short)0)).AsInt32();
                        var int128C = X86.Sse2.UnpackLow(short128B, Vector128.CreateScalar((short)0)).AsInt32();
                        var int128D = X86.Sse2.UnpackHigh(short128B, Vector128.CreateScalar((short)0)).AsInt32();
                        var f128A = X86.Sse2.ConvertToVector128Single(int128A);
                        var f128B = X86.Sse2.ConvertToVector128Single(int128B);
                        var f128C = X86.Sse2.ConvertToVector128Single(int128C);
                        var f128D = X86.Sse2.ConvertToVector128Single(int128D);

                        X86.Sse.Store(fptr + i, f128A);
                        X86.Sse.Store(fptr + i + 4, f128B);
                        X86.Sse.Store(fptr + i + 8, f128C);
                        X86.Sse.Store(fptr + i + 12, f128D);
                        i += 16;
                    }
                }
            }*/
            while (i < bytes.Length)
            {
                floats[i] = bytes[i];
                ++i;
            }
        }
    }
}
