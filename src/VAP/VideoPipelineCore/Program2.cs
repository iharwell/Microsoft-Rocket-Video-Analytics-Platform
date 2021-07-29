// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using BGSObjectDetector;
using DarknetAAB;
using DarknetDetector;
using DNNDetector;
using DNNDetector.Config;
using DNNDetector.Model;
using LineDetector;
using Microsoft.ML;
using MotionTracker;
using OpenCvSharp;
using PostProcessor;
using ProcessingPipeline;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Utils;
using Utils.Items;
using Utils.ShapeTools;

namespace VideoPipelineCore
{
    internal class Program2
    {
        private const int BUFFERSIZE = 300;
        private const int UPDATEPERIOD = 128;
        private const int UPDATEMASK = UPDATEPERIOD - 1;
        private const int GCPERIOD = 256;
        private const int GCMASK = GCPERIOD - 1;

        private const float MergeSimThreshold = 0.5f;
        private const float IoUSimThreshold = 0.20f;
        private const float PolySimThreshold = 0.25f;

        private const bool DisplayToggle = false;

        private static volatile bool running = true;
        private static int RotateCount = 0;
        internal static DateTime TimeStampParser(IFrame f)
        {
            DateTime ts = default;

            if (f.SourceName == null)
            {
                return ts;
            }

            var parts = f.SourceName.Split('_');
            string startString = parts[2];

            if (parts.Length != 5 || startString.Length != 14)
            {
                return ts;
            }

            int year = int.Parse(startString.Substring(0, 4));
            int month = int.Parse(startString.Substring(4, 2));
            int day = int.Parse(startString.Substring(6, 2));
            int hour = int.Parse(startString.Substring(8, 2));
            int minute = int.Parse(startString.Substring(10, 2));
            int second = int.Parse(startString.Substring(12, 2));
            var baseDT = new DateTime(year, month, day, hour, minute, second);
            var VidTime = new TimeSpan((long)(f.FileFrameIndex / f.FrameRate * TimeSpan.TicksPerSecond));
            return baseDT+VidTime;
        }
        internal static string CameraNameParser(IFrame f)
        {
            string cn = null;
            if (f.SourceName == null)
            {
                return cn;
            }
            var parts = f.SourceName.Split('_');

            if (parts.Length != 5)
            {
                return cn;
            }
            string CameraName1 = parts[1];
            string CameraName2 = parts[0];

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(CameraName1);
            sb.Append(" - ");
            sb.Append(CameraName2);

            return sb.ToString();
        }

        internal static void Main(string[] args)
        {
            //parse arguments
            if (args.Length < 5)
            {
                Console.WriteLine(args.Length);
                Console.WriteLine("Usage: <exe> <folder or file> <cfg file> <output folder> <samplingFactor> <resolutionFactor> <category1> <category2> ...");
                return;
            }
            string videoUrl = args[0];
            bool isVideoStream;
            if (videoUrl.Substring(0, 4) == "rtmp" || videoUrl.Substring(0, 4) == "http" || videoUrl.Substring(0, 3) == "mms" || videoUrl.Substring(0, 4) == "rtsp")
            {
                isVideoStream = true;
            }
            else
            {
                isVideoStream = false;
                videoUrl = args[0];
            }
            string lineFile = args[1];
            Utils.Config.OutputFolder.OutputFolderBase = args[2];
            int samplingFactor = int.Parse(args[3]);
            double resolutionFactor = double.Parse(args[4]);

            var decoder = Decoder.DecoderFFMPEG.GetDirectoryDecoder(videoUrl, resolutionFactor, RotateCount);
            decoder.TimeStampParser = TimeStampParser;
            decoder.CameraNameParser = CameraNameParser;

            decoder.BeginReading();
            TrackerPipeline trackerPipeline = new();
            HashSet<string> category = new HashSet<string>();
            for (int i = 5; i < args.Length; i++)
            {
                category.Add(args[i]);
            }

            //initialize pipeline settings
            int pplConfig = Convert.ToInt16(ConfigurationManager.AppSettings["PplConfig"]);
            bool loop = false;
            bool displayRawVideo = false;
            bool displayBGSVideo = false;
            Utils.Utils.CleanFolderAll();

            //-----FramedItem buffer for tracking item paths-----
            IList<IList<IFramedItem>> framedItemBuffer = new List<IList<IFramedItem>>(BUFFERSIZE + 1);

            //-----Decoder-----
            Pipeline pipeline = null;
            try
            {
                SetupPipeline(lineFile, samplingFactor, resolutionFactor, category, displayBGSVideo, out pipeline);

                Action decodeFunc = () =>
                {
                    while (decoder.HasMoreFrames)
                    {
                        var f = decoder.GetNextFrame();
                        pipeline.SyncPostFrame(f);
                    }
                };

                Task decodeTask = new Task(decodeFunc);

                decodeTask.Start();

                pipeline.SpoolPipeline();

                MotionTracker.MotionTracker tracker = SetupTracker(IoUSimThreshold, PolySimThreshold, (float)decoder.FramesPerSecond, resolutionFactor);

                trackerPipeline.Tracker = tracker;
                TimeCompressor compressor = new TimeCompressor(resolutionFactor);
                trackerPipeline.Compressor = compressor;
                trackerPipeline.BufferSize = BUFFERSIZE;
                trackerPipeline.MergeSimilarityThreshold = MergeSimThreshold;
                trackerPipeline.ShouldStartPathFunc = new Func<IFramedItem, bool>((IFramedItem framedItem) =>
                {
                    for (int i = 0; i < framedItem.ItemIDs.Count; i++)
                    {
                        var id = framedItem.ItemIDs[i];
                        object source = id.SourceObject;
                        if (!id.FurtherAnalysisTriggered || source.GetType() != pipeline.LastStage.GetType())
                        {
                            continue;
                        }
                        return true;
                    }
                    return false;
                });

                //-----Last minute prep-----
                DateTime videoTimeStamp;
                if (isVideoStream)
                {
                    videoTimeStamp = DateTime.Now;
                }
                else
                {
                    //TODO(iharwell): Find a portable option for pulling the "Media Created" property from a file for use here.
                    // videoTimeStamp = decoder.getTimeStamp();

                    videoTimeStamp = DateTime.Now;
                }
                double frameRate = decoder.GetVideoFPS();
                int frameIndex = 0;
                int videoTotalFrame = decoder.GetTotalFrameNum();
                int videoNumber = 0;

                IList<IFramedItem> itemList = null;
                IList<IItemPath> itemPaths = new List<IItemPath>();

                //RUN PIPELINE 
                DateTime startTime = DateTime.Now;
                DateTime prevTime = DateTime.Now;
                System.Diagnostics.Stopwatch shortTimer = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch longTimer = new System.Diagnostics.Stopwatch();
                bool firstPassDone = false;
                int prevFrame = 0;
                while (true)
                {

                    if (!loop)
                    {
                        if (!isVideoStream && !(!decodeTask.IsCompleted || pipeline.StillProcessing))
                        {
                            break;
                        }
                    }

                    if(!pipeline.TryReceiveList(out itemList, out var frame))
                    {
                        Thread.Sleep(5);
                        continue;
                    }

                    if (frame == null) continue;

                    frameIndex = frame.FrameIndex;
                    int frameMainIndex = frameIndex;

                    //display counts
                    if (itemList != null && itemList.Count > 0)
                    {
                        Dictionary<string, string> kvpairs = new Dictionary<string, string>();
                        foreach (IFramedItem it in itemList)
                        {
                            foreach (IItemID id in it.ItemIDs)
                            {
                                if (id is ILineTriggeredItemID ltID && ltID.TriggerLine != null)
                                {
                                    if (!kvpairs.ContainsKey(ltID.TriggerLine))
                                        kvpairs.Add(ltID.TriggerLine, "1");
                                    break;
                                }
                            }
                        }
                        FramePreProcessor.FrameDisplay.UpdateKVPairs(kvpairs);
                        trackerPipeline.PostFramedItems(itemList, frameMainIndex);
                    }
                    else
                    {
                        trackerPipeline.PostDummyFrame(frame);
                    }


                    //print out stats
                    if ((frameMainIndex & UPDATEMASK) == 0 && firstPassDone)
                    {
                        double fps = (double)(Stopwatch.Frequency * UPDATEPERIOD) / shortTimer.ElapsedTicks;
                        double avgFps = (double)(Stopwatch.Frequency * (frameMainIndex)) / longTimer.ElapsedTicks;
                        Console.WriteLine("{0} {1,-5} {2} {3,-5} {4} {5,-15} {6} {7,-10:N2} {8} {9,-10:N2}",
                                            "sFactor:", samplingFactor, "rFactor:", resolutionFactor, "FrameID:", frameIndex, "FPS:", fps, "avgFPS:", avgFps);
                        shortTimer.Restart();
                    }
                    if (frameMainIndex != 0 && frameMainIndex != 1 && frameMainIndex != prevFrame && frameMainIndex != prevFrame + 1)
                    {
                        Console.WriteLine("Out of order.");
                    }
                    prevFrame = frameMainIndex;
                    ++frameIndex;

                    if (!firstPassDone)
                    {
                        firstPassDone = true;
                        shortTimer.Restart();
                        longTimer.Restart();
                    }
                }
                /*for (int i = 0; i < itemPaths.Count; i++)
                {
                    Console.WriteLine("Item path length " + (i + 1) + ": " + itemPaths[i].FramedItems.Count);
                }
                IList<IList<IFramedItem>> positiveIds = new List<IList<IFramedItem>>();
                IList<IList<int>> frameNums = new List<IList<int>>();
                foreach (var path in itemPaths)
                {
                    positiveIds.Add(new List<IFramedItem>(GetPositiveIDItems(path)));
                    frameNums.Add(frameNumbers(path));
                }
                for (int i = 0; i < itemPaths.Count; i++)
                {
                    if (itemPaths[i].FramedItems.Count > BUFFERSIZE*2)
                    {
                        compressor.ProcessPath(itemPaths[i]);
                    }
                }*/
                trackerPipeline.WriteAllPaths();
                //WritePaths(itemPaths, tracker, frameRate, ref videoNumber, frameIndex + BUFFERSIZE + 1, BUFFERSIZE, compressor);
            }
            finally
            {
                if (pipeline != null)
                {
                    pipeline.Dispose();
                }

                GC.Collect(3, GCCollectionMode.Forced, true);
                GC.Collect(3, GCCollectionMode.Forced, true);
            }
            Console.WriteLine("Done!");
        }

        private static void SetupPipeline(string lineFile, int samplingFactor, double resolutionFactor, HashSet<string> category, bool displayBGSVideo, out Pipeline pipeline)
        {
            pipeline = new Pipeline();
            var lcProcessor = new LineCrossingProcessor(lineFile, samplingFactor, resolutionFactor, Color.CadetBlue)
            {
                DisplayOutput = displayBGSVideo
            };

            List<LineSegment> preSegments = new List<LineSegment>();
            foreach (var entry in lcProcessor.LineSegments)
            {
                preSegments.Add(entry.Value);
            }

            /*lcProcessor.RotateLines(RotateCount, new System.Drawing.Size(480, 270));*/


            List<LineSegment> postSegments = new List<LineSegment>();
            foreach (var entry in lcProcessor.LineSegments)
            {
                postSegments.Add(entry.Value);
            }
            var bgsStage = new SimpleDNNProcessor(new DarknetAAB.Yolo4Tiny(0))
            {
                BoundingBoxColor = Color.White,
                IncludeCategories = category,
                DisplayOutput = DisplayToggle,
                LineSegments = lcProcessor.LineSegments
            };
            var searchStage = new LineCrossingItemSearchProcessor()
            {
                BufferSize = 150
            };


            pipeline.AppendStage(bgsStage);
            pipeline.AppendStage(lcProcessor);
            pipeline.AppendStage(searchStage);
            var settings = new DataContractJsonSerializerSettings();
            settings.KnownTypes = new List<Type>()
            {
                typeof(Yolo4DNN),
                typeof(Yolo4Tiny),
                typeof(Yolo4Full),
            };
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(pipeline.GetType(),settings);

            FileStream fs = new FileStream("pipeline.json", FileMode.Create);
            serializer.WriteObject(fs, pipeline);
            fs.Flush();
            fs.Close();
        }


        private static MotionTracker.MotionTracker SetupTracker(float iouThreshold, float polyThreshold, float fps, double resolutionFactor)
        {
            var iouPredictor = new Utils.Items.IoUPredictor();
            var fastPredictor = new MotionTracker.FastPiecewisePredictor(30);
            var fastPredictor2 = new MotionTracker.FastPiecewisePredictor(80);
            var piecewise = new Utils.Items.PiecewisePredictor(5.0);
            var polyPredictor = new Utils.Items.CenterPolyPredictor();

            MotionTracker.MotionTracker tracker = new MotionTracker.MotionTracker();


            {
                CategoryFilter filterStage = new CategoryFilter();
                // Driveway Filters
                /*filterStage.ExcludeCategories.Add("umbrella");
                filterStage.ExcludeCategories.Add("skateboard");
                filterStage.ExcludeCategories.Add("skis");
                filterStage.ExcludeCategories.Add("pottedplant");*/

                // Side Filters
                /*filterStage.ExcludeCategories.Add("pottedplant");
                filterStage.ExcludeCategories.Add("suitcase");*/

                // FrontDoor Filters
                /*filterStage.ExcludeCategories.Add("refrigerator");
                filterStage.ExcludeCategories.Add("pottedplant");
                filterStage.ExcludeCategories.Add("book");
                filterStage.ExcludeCategories.Add("tvmonitor");*/

                // FrontYard Filters
                filterStage.ExcludeCategories.Add("fire hydrant");
                filterStage.ExcludeCategories.Add("handbag");
                filterStage.ExcludeCategories.Add("surfboard");

                //filterStage.ExcludeCategories.Add("giraffe");
                //filterStage1.ExcludeCategories.Add("sheep");
                tracker.Filters.Add(filterStage);
            }

            {
                StationaryFilter filterStage = new StationaryFilter()
                {
                    ConfirmationCount = 3,
                    VelocityThreshold = (float)(16f * resolutionFactor),
                    OverlapThreshold = 0.7f,
                    ChunkSize = (int)(0.5f + 3 * fps)
                };
                tracker.Filters.Add(filterStage);
            }

            {
                TriggerIDFilter filterStage = new();
                tracker.Filters.Add(filterStage);
            }

            {
                var filterStage = new IntermittentFilter()
                {
                };
                tracker.Filters.Add(filterStage);
            }


            {
                var filterStage = new CollisionFilter(30, 30, null, 0.2f, 0.4f);
                tracker.Filters.Add(filterStage);
            }



            tracker.Predictors.Add((iouPredictor, iouThreshold));
            tracker.Predictors.Add((fastPredictor, polyThreshold));
            tracker.Predictors.Add((fastPredictor2, polyThreshold));
            tracker.Predictors.Add((piecewise, polyThreshold));
            //tracker.Predictors.Add((polyPredictor,polyThreshold));

            tracker.ManuallyStepFrames = false;
            //tracker.DisplayProcess = true;
            tracker.DisplayProcess = false;

            var settings = new DataContractJsonSerializerSettings();
            settings.KnownTypes = new List<Type>()
            {
                typeof(FastPiecewisePredictor)
            };


            System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(tracker.GetType(), settings);
            FileStream fs = new FileStream("tracker.json", FileMode.Create);
            serializer.WriteObject(fs, tracker);
            fs.Flush();
            fs.Close();

            return tracker;
        }

        private static IEnumerable<IFramedItem> GetPositiveIDItems(IItemPath path)
        {
            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                var item = path.FramedItems[i];
                for (int j = 0; j < item.ItemIDs.Count; j++)
                {
                    var id = item.ItemIDs[j];
                    if (id.Confidence > 0)
                    {
                        yield return item;
                        break;
                    }
                }
            }
        }

        private static IList<int> frameNumbers(IItemPath path)
        {
            List<int> frameNumbers = new List<int>();
            foreach (var framedItem in path.FramedItems)
            {
                frameNumbers.Add(framedItem.Frame.FrameIndex);
            }
            frameNumbers.Sort();
            return frameNumbers;
        }

        private static void WritePaths(IList<IItemPath> paths, MotionTracker.MotionTracker tracker, double frameRate, ref int videoNumber, int currentFrame, int bufferDepth, TimeCompressor compressor)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                while (i < paths.Count && paths.Count > 0 && paths[i].GetPathBounds().maxFrame < currentFrame - bufferDepth)
                {
                    var path = paths[i];

                    if( compressor != null)
                    {
                        compressor.ProcessPath(path);
                    }
                    tracker.ScrubPath(path);
                    videoNumber = WritePath(paths, i, frameRate, videoNumber, path);
                }
            }
        }

        private static int WritePath(IList<IItemPath> paths, int index, double frameRate, int videoNumber, IItemPath path)
        {
            int conFrameIndex = path.HighestConfidenceFrameIndex;
            int conIDIndex = path.HighestConfidenceIDIndex;
            IFrame conframe = path.FramedItems[conFrameIndex].Frame;
            string name = path.FramedItems[conFrameIndex].ItemIDs[conIDIndex].ObjName + " " + videoNumber + ".mp4";

            string output = "output";

            Console.WriteLine($"\tWriting output path to {name} and {output}{videoNumber}.xml");
            // FourCC code = FourCC.X264;
            // FourCC code = FourCC.FromFourChars('x', '2', '6', '4');
            FourCC code = FourCC.FromFourChars('m', 'p', '4', 'v');
            // FourCC code = FourCC.FromFourChars('h', '2', '6', '5');
            OpenCvSharp.VideoWriter writer = new VideoWriter(Utils.Config.OutputFolder.OutputFolderVideo + name, code, frameRate, conframe.FrameData.Size());

            var fItems = from fi in path.FramedItems
                         let f = fi.Frame
                         orderby f.FrameIndex
                         select fi;

            foreach (IFramedItem frameItem in fItems)
            {
                Mat frameData = frameItem.Frame.FrameData.Clone();
                if (!(frameItem.ItemIDs.Count == 1 && frameItem.ItemIDs[0] is FillerID))
                {
                    StatisticRectangle sr = new StatisticRectangle(frameItem.ItemIDs);
                    var median = sr.Median;

                    Cv2.Rectangle(frameData, new Rect((int)median.X, (int)median.Y, (int)median.Width, (int)median.Height), Scalar.Red);
                }

                writer.Write(frameData);
            }
            writer.Release();
            writer.Dispose();
            DataContractSerializer serializer = new DataContractSerializer(typeof(ItemPath));
            if (path is ItemPath p)
            {
                string xmlname = output + videoNumber;
                using StreamWriter swriter = new StreamWriter(Utils.Config.OutputFolder.OutputFolderXML + xmlname + ".xml");
                serializer.WriteObject(swriter.BaseStream, p);
                swriter.Flush();
                swriter.Close();
            }
            ++videoNumber;
            /*if(path is IDisposable disposable)
            {
                disposable.Dispose();
            }*/
            {
                var pa = paths[index];
                for (int i = 0; i < pa.FramedItems.Count; i++)
                {
                    pa.FramedItems[i] = null;
                }
            }

            paths.RemoveAt(index);
            return videoNumber;
        }

        private static void CompressIFrames(IList<IFramedItem> itemList)
        {
            if (itemList is null)
            {
                return;
            }

            for (int i = 0; i < itemList.Count; i++)
            {
                int frameIndex = itemList[i].Frame.FrameIndex;
                for (int j = i + 1; j < itemList.Count; j++)
                {
                    if (itemList[j].Frame.FrameIndex == frameIndex)
                    {
                        if (!object.ReferenceEquals(itemList[j].Frame, itemList[i].Frame))
                        {
                            if(itemList[j].Frame.FrameData != itemList[i].Frame.FrameData && itemList[j].Frame is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                            itemList[j].Frame = itemList[i].Frame;
                        }
                    }
                }
            }
        }

        private static void AddEntryToBuffer(ref IList<IList<IFramedItem>> buffer, IList<IFramedItem> itemsInFrame)
        {

            buffer = Motion.InsertIntoSortedBuffer(buffer, itemsInFrame, MergeSimThreshold, 0);

            while (buffer.Count > BUFFERSIZE || buffer[0].Count == 0)
            {
                buffer.RemoveAt(0);
            }
            buffer = Motion.GroupByFrame(buffer, MergeSimThreshold, MergeSimThreshold / 2);
        }
    }
}
