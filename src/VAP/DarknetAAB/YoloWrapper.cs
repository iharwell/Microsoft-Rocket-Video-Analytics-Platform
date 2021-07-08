using System;
using System.Linq;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace DarknetAAB
{
    public class YoloWrapper : IDisposable
    {
        private const string YoloLibraryName = "darknet.dll";
        internal const int MaxObjects = 1000;

        [DllImport(YoloLibraryName, EntryPoint = "init")]
        private static extern int InitializeYolo(string configurationFilename, string weightsFilename, int gpu);

        [DllImport(YoloLibraryName, EntryPoint = "detect_image")]
        private static extern int DetectImage(string filename, ref BboxContainer container);

        [DllImport(YoloLibraryName, EntryPoint = "detect_mat")]
        private static extern int DetectImage(IntPtr pArray, int nSize, ref BboxContainer container);

        [DllImport(YoloLibraryName, EntryPoint = "dispose")]
        private static extern int DisposeYolo();

        public YoloWrapper(string configurationFilename, string weightsFilename, int gpu)
        {
            InitializeYolo(configurationFilename, weightsFilename, gpu);
        }

        public void Dispose()
        {
            DisposeYolo();
        }

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
            byte[] imgBuffer = imageData.ToBytes(".bmp");
            bbox_t[] yoloItems;

            fixed (byte* ptr = &imgBuffer[0])
            {
                yoloItems = TrackUnmanaged((IntPtr)ptr, imgBuffer.Length);
            }
            if (yoloItems == null || !yoloItems.Any())
            {
                return null;
            }

            return yoloItems;
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
    }
}
