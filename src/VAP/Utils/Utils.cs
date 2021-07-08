// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using OpenCvSharp;
using Utils.Items;
using Point = System.Drawing.Point;
namespace Utils
{
    public class Utils
    {
        public static void CleanFolder(string folder)
        {
            Directory.CreateDirectory(folder);
            DirectoryInfo di = new DirectoryInfo(folder);
            foreach (FileInfo file in di.GetFiles()) file.Delete();
        }

        public static void CleanFolderAll()
        {
            CleanFolder(Config.OutputFolder.OutputFolderAll);
            CleanFolder(Config.OutputFolder.OutputFolderVideo);
            CleanFolder(Config.OutputFolder.OutputFolderXML);
            CleanFolder(Config.OutputFolder.OutputFolderBGSLine);
            CleanFolder(Config.OutputFolder.OutputFolderLtDNN);
            CleanFolder(Config.OutputFolder.OutputFolderCcDNN);
            CleanFolder(Config.OutputFolder.OutputFolderAML);
            CleanFolder(Config.OutputFolder.OutputFolderFrameDNNDarknet);
            CleanFolder(Config.OutputFolder.OutputFolderFrameDNNTF);
            CleanFolder(Config.OutputFolder.OutputFolderFrameDNNONNX);
        }

        public static byte[] MatToByteBmp(Mat image)
        {
            // known good:
            // return ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image));
            return image.ToBytes(".bmp");
        }

        public static byte[] ImageToByteBmp(Image imageIn)
        {
            using var ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms.ToArray();
        }

        public static byte[] ImageToByteJpeg(Image imageIn)
        {
            using var ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.ToArray();
        }

        public static float CheckLineBboxOverlapRatio(int[] line, int bbox_x, int bbox_y, int bbox_w, int bbox_h)
        {
            LineSegment newLine = new LineSegment(new Point(line[0], line[1]), new Point(line[2], line[3]));
            return CheckLineBboxOverlapRatio(newLine, bbox_x, bbox_y, bbox_w, bbox_h);
        }
        public static float CheckLineBboxOverlapRatio(int[] line, Rectangle bbox)
        {
            LineSegment newLine = new LineSegment(new Point(line[0], line[1]), new Point(line[2], line[3]));
            return CheckLineBboxOverlapRatio(newLine, bbox.X, bbox.Y, bbox.Width, bbox.Height);
        }

        public static float CheckLineBboxOverlapRatio(LineSegment line, Rectangle bbox)
        {
            return CheckLineBboxOverlapRatio(line, bbox.X, bbox.Y, bbox.Width, bbox.Height);
        }

        public static float CheckLineBboxOverlapRatio(LineSegment line, int bbox_x, int bbox_y, int bbox_w, int bbox_h)
        {
            int insidePixels = 0;

            IEnumerable<Point> linePixels = EnumerateLineNoDiagonalSteps(line.P1, line.P2);

            foreach (Point pixel in linePixels)
            {
                if ((pixel.X >= bbox_x) && (pixel.X <= bbox_x + bbox_w) && (pixel.Y >= bbox_y) && (pixel.Y <= bbox_y + bbox_h))
                {
                    insidePixels++;
                }
            }

            var overlapRatio = (float)insidePixels / linePixels.Count();
            return overlapRatio;
        }

        private static IEnumerable<Point> EnumerateLineNoDiagonalSteps(Point p0, Point p1)
        {
            int dx = Math.Abs(p1.X - p0.X), sx = p0.X < p1.X ? 1 : -1;
            int dy = -Math.Abs(p1.Y - p0.Y), sy = p0.Y < p1.Y ? 1 : -1;
            int err = dx + dy, e2;

            while (true)
            {
                yield return p0;

                if (p0.X == p1.X && p0.Y == p1.Y) break;

                e2 = 2 * err;

                // EITHER horizontal OR vertical step (but not both!)
                if (e2 > dy)
                {
                    err += dy;
                    p0.X += sx;
                }
                else if (e2 < dx)
                { // <--- this "else" makes the difference
                    err += dx;
                    p0.Y += sy;
                }
            }
        }

        public static byte[] DrawImage(byte[] imageData, int x, int y, int w, int h, Color color, string annotation = "")
        {
            using var memoryStream = new MemoryStream(imageData);
            using var image = Image.FromStream(memoryStream);
            using var canvas = Graphics.FromImage(image);
            using var pen = new Pen(color, 3);
            canvas.DrawRectangle(pen, x, y, w, h);
            canvas.DrawString(annotation, new Font("Arial", 16), new SolidBrush(color), new PointF(x, y - 20));
            canvas.Flush();

            using var memoryStream2 = new MemoryStream();
            image.Save(memoryStream2, ImageFormat.Bmp);
            return memoryStream2.ToArray();
        }
        public static byte[] DrawImage(Mat imageData, int x, int y, int w, int h, Color color, string annotation = "")
        {
            Mat output = imageData.Clone();
            Cv2.Rectangle(output, new Rect(x, y, w, h), new Scalar(color.B, color.G, color.R), 2);
            if (annotation != null && annotation.Length > 0)
            {
                Cv2.PutText(output, annotation, new OpenCvSharp.Point(x, y - 20), HersheyFonts.HersheyPlain, 16, new Scalar(0, color.B, color.G, color.R));
            }
            /*canvas.DrawRectangle(pen, x, y, w, h);
            canvas.DrawString(annotation, new Font("Arial", 16), new SolidBrush(color), new PointF(x, y - 20));
            canvas.Flush();

            using var memoryStream2 = new MemoryStream();
            image.Save(memoryStream2, ImageFormat.Bmp);*/
            return output.ToBytes(".bmp");
        }

        public static IEnumerable<IFramedItem> GetItemsForFurtherProcessing(IEnumerable<IFramedItem> items)
        {
            foreach (var item in items)
            {
                IItemID lastID = item.ItemIDs[^1];
                if (lastID.FurtherAnalysisTriggered)
                {
                    yield return item;
                }
            }
        }

        public static byte[] CropImage(byte[] imageData, int x, int y, int w, int h)
        {
            using var memoryStream = new MemoryStream(imageData);
            using var image = Image.FromStream(memoryStream);
            Rectangle cropRect = new Rectangle(x, y, Math.Min(image.Width - x, w), Math.Min(image.Height - y, h));
            Bitmap bmpImage = new Bitmap(image);
            Image croppedImage = bmpImage.Clone(cropRect, bmpImage.PixelFormat);

            using var memoryStream2 = new MemoryStream();
            croppedImage.Save(memoryStream2, ImageFormat.Bmp);
            return memoryStream2.ToArray();
        }

        public static byte[] CropImage(Mat imageData, int x, int y, int w, int h)
        {
            return imageData.SubMat(y, Math.Min(y + h, imageData.Rows - 1), x, Math.Min(x + w, imageData.Cols - 1)).ToBytes(".bmp");

            /*using var memoryStream = new MemoryStream(imageData);
            using var image = Image.FromStream(memoryStream);
            Rectangle cropRect = new Rectangle(x, y, Math.Min(image.Width - x, w), Math.Min(image.Height - y, h));
            Bitmap bmpImage = new Bitmap(image);
            Image croppedImage = bmpImage.Clone(cropRect, bmpImage.PixelFormat);

            using var memoryStream2 = new MemoryStream();
            croppedImage.Save(memoryStream2, ImageFormat.Bmp);
            return memoryStream2.ToArray();*/
        }

        public static List<Tuple<string, int[]>> ConvertLines(List<(string key, LineSegment coordinates)> lines)
        {
            List<Tuple<string, int[]>> newLines = new List<Tuple<string, int[]>>();
            foreach ((string key, LineSegment coordinates) in lines)
            {
                int[] coor = new int[] { coordinates.P1.X, coordinates.P1.Y, coordinates.P2.X, coordinates.P2.Y };
                Tuple<string, int[]> newLine = new Tuple<string, int[]>(key, coor);
                newLines.Add(newLine);
            }
            return newLines;
        }

        public static Dictionary<string, int> CatHashSet2Dict(HashSet<string> cat)
        {
            Dictionary<string, int> catDict = new Dictionary<string, int>();
            foreach (string c in cat)
            {
                catDict.Add(c, 0);
            }
            return catDict;
        }

        public static void WriteAllBytes(string path, Mat data)
        {
            Stream s = new FileStream(path, FileMode.OpenOrCreate);
            data.WriteToStream(s, ".bmp");
            s.Flush();
            s.Close();
        }
        public static void SaveFoundItemImage(string[] folderNames, string fileName, IFramedItem framedItem, int idIndex, Color taggedImageColor)
        {
            var outImage = framedItem.TaggedImageData(idIndex, taggedImageColor);
            // output cheap YOLO results
            //string blobName_Cheap = $@"frame-{framedItem.Frame.FrameIndex}-Cheap-{framedItem.ItemIDs[idIndex]}.jpg";

            for (int i = 0; i < folderNames.Length; i++)
            {
                string filePath = folderNames[i] + fileName;
                FileStream fstream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                outImage.WriteToStream(fstream, ".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
                fstream.Flush();
                fstream.Close();
                fstream.Dispose();
            }
            outImage.Dispose();
        }
    }
}
