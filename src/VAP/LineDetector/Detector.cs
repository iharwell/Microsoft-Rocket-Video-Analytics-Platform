// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenCvSharp;
using Utils.Config;
using BGSObjectDetector;
using Utils;
using Utils.Items;

namespace LineDetector
{
    public class Detector
    {
        public bool DISPLAY_BGS { get; set; }

        /// <summary>
        /// The initial delay to allow for the background subtractor to kick in, N_FRAMES_TO_LEARN in MOG2.cs
        /// </summary>
        public int StartDelay { get; set; } = 120;


        public MultiLaneDetector _multiLaneDetector;

        private Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _counts_prev = new Dictionary<string, int>();
        private Dictionary<string, bool> _occupancy = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _occupancy_prev = new Dictionary<string, bool>();

        /// <summary>
        /// Constructs a <see cref="Detector"/> object with the provided 
        /// </summary>
        /// <param name="sFactor">Sampling rate scaling factor.</param>
        /// <param name="rFactor">Resolution scaling factor.</param>
        /// <param name="linesFile">A file specifying the lines used for the line-crossing algorithm.</param>
        /// <param name="displayBGS">True to display a separate image each frame with the current frame number, the lines used, and the changes from the previous frame.</param>
        public Detector(int sFactor, double rFactor, string linesFile, bool displayBGS)
        {
            Dictionary<string, ILineBasedDetector> lineBasedDetectors = LineSets.ReadLineSet_LineDetector_FromTxtFile(linesFile, sFactor, rFactor);

            _multiLaneDetector = (lineBasedDetectors != null) ? new MultiLaneDetector(lineBasedDetectors) : null;

            this.DISPLAY_BGS = displayBGS;
            Console.WriteLine(linesFile);
        }

        /// <summary>
        /// Checks for items crossing the provided LineSet.
        /// </summary>
        /// <param name="frame">The frame to check.</param>
        /// <param name="frameIndex">The index of the frame given.</param>
        /// <param name="fgmask">The foreground mask of the frame.</param>
        /// <param name="boxes">A list of bounding boxes of items in the frame which deviate from the background.</param>
        /// <returns>
        ///   <para>Returns a tuple with two <see cref="Dictionary{TKey, TValue}">Dictionaries</see>.</para>
        ///   <para>The first dictionary contains the number of items which cross the lines of interest, indexed by line name.</para>
        ///   <para>The second dictionary contains a boolean for each line indicating whether or not an item is present at that line.</para>
        /// </returns>
        public (Dictionary<string, int>, Dictionary<string, bool>) UpdateLineResults(Mat frame, int frameIndex, Mat fgmask, IList<IFramedItem> boxes, object sourceObject = null)
        {
            if (frameIndex > StartDelay)
            {
                IFrame fr = new Frame(null, frameIndex, frame)
                {
                    ForegroundMask = fgmask
                };
                //Bitmap fgmaskBit = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(fgmask);

                //multiLaneDetector.notifyFrameArrival( frameIndex, boxes, fgmaskBit );
                _multiLaneDetector.notifyFrameArrival(fr, frameIndex, boxes, fgmask, sourceObject);

                // bgs visualization with lines
                if (DISPLAY_BGS)
                {
                    List<(string key, LineSegment coordinates)> lines = this._multiLaneDetector.getAllLines();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        System.Drawing.Point p1 = lines[i].coordinates.P1;
                        System.Drawing.Point p2 = lines[i].coordinates.P2;
                        Cv2.Line(fgmask, p1.X, p1.Y, p2.X, p2.Y, new Scalar(255, 0, 255, 255), 2);
                    }
                    Cv2.ImShow("BGS Output", fgmask);
                    //Cv2.WaitKey(1);
                }
            }
            _counts = _multiLaneDetector.getCounts();

            if (_counts_prev.Count != 0)
            {
                foreach (string lane in _counts.Keys)
                {
                    int diff = Math.Abs(_counts[lane] - _counts_prev[lane]);
                    if (diff > 0) //object detected by BGS-based counter
                    {
                        Console.WriteLine($"Line: {lane}\tCounts: {_counts[lane]}");
                        string blobName_BGS = $@"frame-{frameIndex}-BGS-{lane}-{_counts[lane]}.jpg";
                        string fileName_BGS = @OutputFolder.OutputFolderBGSLine + blobName_BGS;
                        frame.SaveImage(fileName_BGS);
                        frame.SaveImage(@OutputFolder.OutputFolderAll + blobName_BGS);
                    }
                }
            }
            UpdateCount(_counts);

            //occupancy
            _occupancy = _multiLaneDetector.getOccupancy();
            foreach (string lane in _occupancy.Keys)
            {
                //output frames that have line occupied by objects
                //if (frameIndex > 1)
                //{
                //    if (occupancy[lane])
                //    {
                //        string blobName_BGS = $@"frame-{frameIndex}-BGS-{lane}-{occupancy[lane]}.jpg";
                //        string fileName_BGS = @OutputFolder.OutputFolderBGSLine + blobName_BGS;
                //        frame.SaveImage(fileName_BGS);
                //        frame.SaveImage(@OutputFolder.OutputFolderAll + blobName_BGS);
                //    }
                //}
                UpdateCount(lane, _occupancy);
            }

            return (_counts, _occupancy);
        }

        public IList<IFramedItem> UpdateLineResults(IFrame frame, IList<IFramedItem> boxes, object signature)
        {
            if (frame.FrameIndex > StartDelay)
            {
                _multiLaneDetector.notifyFrameArrival(frame, frame.FrameIndex, boxes, frame.ForegroundMask, signature);

                // bgs visualization with lines
                if (DISPLAY_BGS)
                {
                    List<(string key, LineSegment coordinates)> lines = this._multiLaneDetector.getAllLines();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        System.Drawing.Point p1 = lines[i].coordinates.P1;
                        System.Drawing.Point p2 = lines[i].coordinates.P2;
                        Cv2.Line(frame.ForegroundMask, p1.X, p1.Y, p2.X, p2.Y, new Scalar(255, 0, 255, 255), 5);
                    }
                    Cv2.ImShow("BGS Output", frame.ForegroundMask);
                    Cv2.WaitKey(1);
                }
            }
            _counts = _multiLaneDetector.getCounts();

            if (_counts_prev.Count != 0)
            {
                foreach (string lane in _counts.Keys)
                {
                    int diff = Math.Abs(_counts[lane] - _counts_prev[lane]);
                    if (diff > 0) //object detected by BGS-based counter
                    {
                        Console.WriteLine($"Line: {lane}\tCounts: {_counts[lane]}");
                        string blobName_BGS = $@"frame-{frame.FrameIndex}-BGS-{lane}-{_counts[lane]}.jpg";
                        string fileName_BGS = @OutputFolder.OutputFolderBGSLine + blobName_BGS;
                        frame.FrameData.SaveImage(fileName_BGS);
                        frame.FrameData.SaveImage(@OutputFolder.OutputFolderAll + blobName_BGS);
                    }
                }
            }
            UpdateCount(_counts);

            //occupancy
            _occupancy = _multiLaneDetector.getOccupancy();
            foreach (string lane in _occupancy.Keys)
            {
                //output frames that have line occupied by objects
                //if (frameIndex > 1)
                //{
                //    if (occupancy[lane])
                //    {
                //        string blobName_BGS = $@"frame-{frameIndex}-BGS-{lane}-{occupancy[lane]}.jpg";
                //        string fileName_BGS = @OutputFolder.OutputFolderBGSLine + blobName_BGS;
                //        frame.SaveImage(fileName_BGS);
                //        frame.SaveImage(@OutputFolder.OutputFolderAll + blobName_BGS);
                //    }
                //}
                UpdateCount(lane, _occupancy);
            }

            return boxes;

            throw new NotImplementedException();
            //return (counts, occupancy);
        }
        public (Dictionary<string, int>, Dictionary<string, bool>) UpdateLineResults2(IFrame frame, IList<IFramedItem> boxes, object signature)
        {
            if (frame.FrameIndex > StartDelay)
            {
                _multiLaneDetector.notifyFrameArrival(frame, frame.FrameIndex, boxes, frame.ForegroundMask, signature);

                // bgs visualization with lines
                if (DISPLAY_BGS)
                {
                    List<(string key, LineSegment coordinates)> lines = this._multiLaneDetector.getAllLines();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        System.Drawing.Point p1 = lines[i].coordinates.P1;
                        System.Drawing.Point p2 = lines[i].coordinates.P2;
                        Cv2.Line(frame.ForegroundMask, p1.X, p1.Y, p2.X, p2.Y, new Scalar(255, 0, 255, 255), 5);
                    }
                    Cv2.ImShow("BGS Output", frame.ForegroundMask);
                    Cv2.WaitKey(1);
                }
            }
            _counts = _multiLaneDetector.getCounts();

            if (_counts_prev.Count != 0)
            {
                foreach (string lane in _counts.Keys)
                {
                    int diff = Math.Abs(_counts[lane] - _counts_prev[lane]);
                    if (diff > 0) //object detected by BGS-based counter
                    {
                        Console.WriteLine($"Line: {lane}\tCounts: {_counts[lane]}");
                        string blobName_BGS = $@"frame-{frame.FrameIndex}-BGS-{lane}-{_counts[lane]}.jpg";
                        string fileName_BGS = @OutputFolder.OutputFolderBGSLine + blobName_BGS;
                        frame.FrameData.SaveImage(fileName_BGS);
                        frame.FrameData.SaveImage(@OutputFolder.OutputFolderAll + blobName_BGS);
                    }
                }
            }
            UpdateCount(_counts);

            //occupancy
            _occupancy = _multiLaneDetector.getOccupancy();
            foreach (string lane in _occupancy.Keys)
            {
                //output frames that have line occupied by objects
                //if (frameIndex > 1)
                //{
                //    if (occupancy[lane])
                //    {
                //        string blobName_BGS = $@"frame-{frameIndex}-BGS-{lane}-{occupancy[lane]}.jpg";
                //        string fileName_BGS = @OutputFolder.OutputFolderBGSLine + blobName_BGS;
                //        frame.SaveImage(fileName_BGS);
                //        frame.SaveImage(@OutputFolder.OutputFolderAll + blobName_BGS);
                //    }
                //}
                UpdateCount(lane, _occupancy);
            }
            return (_counts, _occupancy);
        }

        /*
        private bool occupancyChanged(string lane)
        {
            bool diff = false;
            if (_occupancy_prev.Count != 0)
            {
                diff = _occupancy[lane] != _occupancy_prev[lane];
            }

            return diff;
        }
        */

        private void UpdateCount(string lane, Dictionary<string, bool> counts)
        {
            _occupancy_prev[lane] = counts[lane];
        }

        private void UpdateCount(Dictionary<string, int> counts)
        {
            foreach (string dir in counts.Keys)
            {
                _counts_prev[dir] = counts[dir];
            }
        }
    }
}
