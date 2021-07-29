using System;
using System.Linq;
using System.Runtime.InteropServices;
using OpenCvSharp;
using System.Runtime.Intrinsics;
using X86 = System.Runtime.Intrinsics.X86;

namespace DarknetAAB
{
    public class YoloWrapper : IDisposable
    {
        private const string YoloLibraryName = "darknet.dll";
        internal const int MaxObjects = 1000;
        private bool _disposedValue;

        [DllImport(YoloLibraryName, EntryPoint = "init")]
        private static extern int InitializeYolo(string configurationFilename, string weightsFilename, int gpu);

        [DllImport(YoloLibraryName, EntryPoint = "detect_image")]
        private static extern int DetectImage(string filename, ref BboxContainer container);

        [DllImport(YoloLibraryName, EntryPoint = "detect_mat")]
        private static extern int DetectImage(IntPtr pArray, int nSize, ref BboxContainer container);

        [DllImport(YoloLibraryName, EntryPoint = "dispose")]
        private static extern int DisposeYolo();

        [DllImport(YoloLibraryName, EntryPoint = "detect_opencv")]
        private static extern int DetectOpenCv(IntPtr data, ref BboxContainer container);

        public YoloWrapper(string configurationFilename, string weightsFilename, int gpu)
        {
            int ret = InitializeYolo(configurationFilename, weightsFilename, gpu);
            if (ret < 0)
            {
                throw new Exception($"Error code {ret.ToString("X")} thrown.");
            }
        }

        /*public void Dispose()
        {
            int ret = DisposeYolo();
            if (ret < 0)
            {
                throw new Exception($"Error code {ret.ToString("X")} thrown.");
            }
            GC.SuppressFinalize(this);
        }*/

        public bbox_t[] Detect(string filename)
        {
            var container = new BboxContainer();
            var count = DetectImage(filename, ref container);

            return container.candidates;
        }

        public bbox_t[] Detect(byte[] imageData)
        {
            var container = new BboxContainer();

            var size = Marshal.SizeOf(imageData[0]) * imageData.Length;
            var pnt = Marshal.AllocHGlobal(size);

            try
            {
                // Copy the array to unmanaged memory.
                Marshal.Copy(imageData, 0, pnt, imageData.Length);
                var count = DetectImage(pnt, imageData.Length, ref container);
                if (count == -1)
                {
                    throw new NotSupportedException($"{YoloLibraryName} has no OpenCV support");
                }
            }
            catch (Exception exception)
            {
                return null;
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(pnt);
            }

            return container.candidates;
        }

        public unsafe bbox_t[] Detect(Mat imageData)
        {
            bbox_t[] yoloItems;
            BboxContainer container = new BboxContainer();
            /*int count;
            try
            {
                count = DetectOpenCv(imageData.CvPtr, ref container);
            }
            catch (Exception)
            {
                return null;
            }
            yoloItems = new bbox_t[count];

            for (int i = 0; i < count; i++)
            {
                yoloItems[i] = container.candidates[i];
            }
            return yoloItems;*/
            imgBuffer = imageData.ToBytes(".bmp");
            fixed (byte* ptr = imgBuffer)
            {
                yoloItems = TrackUnmanaged((IntPtr)ptr, imgBuffer.Length);
            }
            if (yoloItems == null || !yoloItems.Any())
            {
                return null;
            }

            return yoloItems;
        }

        private static unsafe void CvtArray(byte[] bytes, float[] floats)
        {
            int i = 0;

            if (X86.Avx2.IsSupported)
            {
                fixed (byte* bptr = bytes)
                fixed (float* fptr = floats)
                {
                    var loadVec = Vector256.Create(0, 2, 4, 6, 1, 3, 5, 7);
                    while (i + 31 < bytes.Length)
                    {

                        //var byteSrc = X86.Avx.LoadVector256(bptr + i);
                        //var byteSrc = X86.Sse2.LoadVector128(bptr + i);
                        var byteSrc = X86.Avx2.GatherVector256((int*)(bptr + i), loadVec, 4).AsByte();
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
            else if (X86.Sse2.IsSupported)
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

        public bbox_t[] TrackUnmanaged(IntPtr data, int dataSize)
        {
            var container = new BboxContainer();
            int count = 0;
            try
            {
                count = DetectImage(data, dataSize, ref container);


            }
            catch (Exception)
            {
                return null;
            }
            bbox_t[] results = new bbox_t[count];

            for (int i = 0; i < count; i++)
            {
                results[i] = container.candidates[i];
            }

            return results;
        }

        private byte[] imgBuffer { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                int ret = DisposeYolo();
                if (ret < 0)
                {
                    throw new Exception($"Error code {ret.ToString("X")} thrown.");
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        ~YoloWrapper() => Dispose(false);

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~YoloWrapper()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
