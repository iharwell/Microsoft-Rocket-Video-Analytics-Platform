// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using AML.Client;
using BGSObjectDetector;
using DarknetDetector;
using DNNDetector;
using DNNDetector.Config;
using DNNDetector.Model;
using LineDetector;
using MotionTracker;
using OpenCvSharp;
using PostProcessor;
using ProcessingPipeline;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using TFDetector;
using Utils;
using Utils.Items;
using Utils.ShapeTools;

namespace VideoPipelineCore
{
    internal class Program2
    {

        private const int BUFFERSIZE = 180;
        private const int UPDATEPERIOD = 128;
        private const int UPDATEMASK = UPDATEPERIOD - 1;
        private const int GCPERIOD = 256;
        private const int GCMASK = GCPERIOD - 1;

        private const double MergeSimThreshold = 0.2;
        private const double IoUSimThreshold = 0.15;
        private const double PolySimThreshold = 0.15;

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

            return new DateTime(year, month, day, hour, minute, second);
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
            var decoder = Decoder.DecoderFFMPEG.GetDirectoryDecoder(videoUrl, resolutionFactor);
            decoder.TimeStampParser = TimeStampParser;
            decoder.CameraNameParser = CameraNameParser;
            decoder.BeginReading();

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

            var polyPredictor = new Utils.Items.CenterPolyPredictor();
            var ioUPredictor = new Utils.Items.PiecewisePredictor(8.0);

            //-----FramedItem buffer for tracking item paths-----
            IList<IList<IFramedItem>> framedItemBuffer = new List<IList<IFramedItem>>(BUFFERSIZE + 1);

            //-----Decoder-----
            //Decoder.Decoder2V decoder = new Decoder.Decoder2V(videoUrl, resolutionFactor, loop);

            Pipeline pipeline = new Pipeline();
            PreProcessorStage bgsStage = new PreProcessorStage
            {
                SamplingFactor = samplingFactor,
                ResolutionFactor = resolutionFactor,
                BoundingBoxColor = Color.White,
                Categories = category,

                DisplayOutput = displayRawVideo
            };
            pipeline.AppendStage(bgsStage);

            LineCrossingProcessor lcProcessor = new LineCrossingProcessor(lineFile, samplingFactor, resolutionFactor, category, Color.CadetBlue)
            {
                DisplayOutput = displayBGSVideo
            };
            pipeline.AppendStage(lcProcessor);

            LightDarknetProcessor lightDNProcessor = new LightDarknetProcessor(lcProcessor.LineSegments, category, Color.Pink, false)
            {
                DisplayOutput = true,
                IndexChooser = new MotionTracker.SparseIndexChooser()
            };
            pipeline.AppendStage(lightDNProcessor);

            HeavyDarknetProcessor heavyDNProcessor = new HeavyDarknetProcessor(lcProcessor.LineSegments, category, resolutionFactor, Color.Red, false);
            pipeline.AppendStage(heavyDNProcessor);

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
            //string lastStageName = pipeline.LastStage.GetType().Name;
            //string secondToLastStageName = pipeline[pipeline.Count-2].GetType().Name;
            object lastStage = pipeline.LastStage;
            object secondToLastStage = pipeline[^2];

            //pipeline.SpoolPipeline();
            //RUN PIPELINE 
            DateTime startTime = DateTime.Now;
            DateTime prevTime = DateTime.Now;

            int prevFrame = 0;
            while (true)
            {
                if (!loop)
                {
                    if (!isVideoStream && !decoder.HasMoreFrames)
                    {
                        break;
                    }
                }
                itemList = null;
                //decoder
                IFrame frame = decoder.GetNextFrame();

                if (frame.FrameData == null)
                {
                    continue;
                }
                frame.FrameIndex = frameIndex;
                if (frame.TimeStamp == default)
                {
                    frame.TimeStamp = startTime.AddSeconds((frameIndex * 1.0 / frameRate));
                }

                itemList = pipeline.ProcessFrame(frame);

                int frameMainIndex = frameIndex;
                /*pipeline.SyncPostFrame(frame);

                if( !pipeline.TryReceiveList(out itemList,out frameOut) )
                {
                    frameIndex++;
                    continue;
                }

                int frameMainIndex = frameOut.FrameIndex;*/

                //itemList = pipeline.ProcessFrame(frame);

                if (frame == null) continue;


                if ((frameMainIndex & GCMASK) == 0)
                {
                    GC.Collect();
                }

                //Merge IFrame items to conserve memory.
                CompressIFrames(itemList);

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
                    lastStage = pipeline.LastStage;
                    secondToLastStage = pipeline[^2];
                    bool pathFound = false;
                    for (int i = 0; i < itemPaths.Count; i++)
                    {
                        if (MotionTracker.MotionTracker.TestAndAdd(itemList, ioUPredictor, itemPaths[i], IoUSimThreshold))
                        {
                            MotionTracker.MotionTracker.SealPath(itemPaths[i], framedItemBuffer);
                        }
                        else if (MotionTracker.MotionTracker.TestAndAdd(itemList, polyPredictor, itemPaths[i], PolySimThreshold))
                        {
                            MotionTracker.MotionTracker.SealPath(itemPaths[i], framedItemBuffer);
                        }/**/
                    }

                    foreach (var item in itemList)
                    {
                        object source = item.ItemIDs.Last().SourceObject;
                        // string lastMethod = ItemList.Last().ItemIDs.Last().IdentificationMethod;
                        // Currently requires the use of a detection line filter, as it generates too many ItemPaths without it.
                        if (!item.ItemIDs.Last().FurtherAnalysisTriggered)
                        {
                            continue;
                        }

                        if (source.GetType() == lastStage.GetType() || source.GetType() == secondToLastStage.GetType())
                        {
                            var path = MotionTracker.MotionTracker.GetPathFromIdAndBuffer(item, framedItemBuffer, ioUPredictor, IoUSimThreshold);
                            int prevCount;
                            int currentCount = path.FramedItems.Count;
                            do
                            {
                                prevCount = currentCount;
                                MotionTracker.MotionTracker.ExpandPathFromBuffer(path, framedItemBuffer, polyPredictor, PolySimThreshold);
                                currentCount = path.FramedItems.Count;

                                if (currentCount > prevCount)
                                {
                                    prevCount = currentCount;
                                    MotionTracker.MotionTracker.ExpandPathFromBuffer(path, framedItemBuffer, ioUPredictor, IoUSimThreshold);
                                    currentCount = path.FramedItems.Count;
                                }
                            } while (currentCount > prevCount)
                                ;
                            MotionTracker.MotionTracker.SealPath(path, framedItemBuffer);
                            itemPaths.Add(path);
                            pathFound = true;
                        }
                    }
                    if (pathFound)
                    {
                        MotionTracker.MotionTracker.TryMergePaths(ref itemPaths, MergeSimThreshold);
                    }

                    MotionTracker.MotionTracker.InsertIntoSortedBuffer(framedItemBuffer, itemList, MergeSimThreshold, MergeSimThreshold / 2);

                    WritePaths(itemPaths, frameRate, ref videoNumber, frameMainIndex, BUFFERSIZE);

                    while (framedItemBuffer.Count > BUFFERSIZE || framedItemBuffer[0].Count == 0)
                    {
                        framedItemBuffer.RemoveAt(0);
                    }
                    //framedItemBuffer = MotionTracker.MotionTracker.GroupByFrame(framedItemBuffer, MergeSimThreshold, MergeSimThreshold / 2);
                }
                else
                {
                    List<IFramedItem> dummylist = new List<IFramedItem>();
                    IFramedItem dummyItem = new FramedItem
                    {
                        Frame = frame
                    };
                    dummylist.Add(dummyItem);
                    MotionTracker.MotionTracker.InsertIntoSortedBuffer(framedItemBuffer, itemList, MergeSimThreshold, MergeSimThreshold / 2);
                    while (framedItemBuffer.Count > BUFFERSIZE || (framedItemBuffer.Count > 0 && framedItemBuffer[0].Count == 0))
                    {
                        framedItemBuffer.RemoveAt(0);
                    }
                }


                //print out stats
                if ((frameMainIndex & UPDATEMASK) == 0)
                {
                    double fps = 1000 * (double)(UPDATEPERIOD) / (DateTime.Now - prevTime).TotalMilliseconds;
                    /*if (fps > 1000 )
                    {
                        Console.Write("Weird ");
                    }*/
                    double avgFps = 1000 * (long)frameMainIndex / (DateTime.Now - startTime).TotalMilliseconds;
                    Console.WriteLine("{0} {1,-5} {2} {3,-5} {4} {5,-15} {6} {7,-10:N2} {8} {9,-10:N2}",
                                        "sFactor:", samplingFactor, "rFactor:", resolutionFactor, "FrameID:", frameIndex, "FPS:", fps, "avgFPS:", avgFps);
                    prevTime = DateTime.Now;
                }
                if (frameMainIndex != 0 && frameMainIndex != 1 && frameMainIndex != prevFrame && frameMainIndex != prevFrame + 1)
                {
                    Console.WriteLine("Out of order.");
                }
                prevFrame = frameMainIndex;
                ++frameIndex;
            }
            for (int i = 0; i < itemPaths.Count; i++)
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
            WritePaths(itemPaths, frameRate, ref videoNumber, frameIndex + BUFFERSIZE + 1, BUFFERSIZE);
            Console.WriteLine("Done!");
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

        private static void WriteVideos(ref IList<IItemPath> paths, double frameRate)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                int conFrameIndex = path.HighestConfidenceFrameIndex;
                int conIDIndex = path.HighestConfidenceIDIndex;
                IFrame conframe = path.FramedItems[conFrameIndex].Frame;
                string name = path.FramedItems[conFrameIndex].ItemIDs[conIDIndex].ObjName + " " + i + ".mp4";
                OpenCvSharp.VideoWriter writer = new VideoWriter(Utils.Config.OutputFolder.OutputFolderVideo + name, FourCC.H265, frameRate, conframe.FrameData.Size());

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
            }
        }
        private static void WritePaths(IList<IItemPath> paths, double frameRate, ref int videoNumber, int currentFrame, int bufferDepth)
        {
            while (paths.Count > 0 && paths[0].GetPathBounds().maxFrame < currentFrame - bufferDepth)
            {
                var path = paths[0];
                int conFrameIndex = path.HighestConfidenceFrameIndex;
                int conIDIndex = path.HighestConfidenceIDIndex;
                IFrame conframe = path.FramedItems[conFrameIndex].Frame;
                string name = path.FramedItems[conFrameIndex].ItemIDs[conIDIndex].ObjName + " " + videoNumber + ".mp4";
                OpenCvSharp.VideoWriter writer = new VideoWriter(Utils.Config.OutputFolder.OutputFolderVideo + name, FourCC.H265, frameRate, conframe.FrameData.Size());

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

                string output = "output";
                DataContractSerializer serializer = new DataContractSerializer(typeof(ItemPath));
                if (paths[0] is ItemPath p)
                {
                    string xmlname = output + videoNumber;
                    using StreamWriter swriter = new StreamWriter(Utils.Config.OutputFolder.OutputFolderXML + xmlname + ".xml");
                    serializer.WriteObject(swriter.BaseStream, p);
                    swriter.Flush();
                    swriter.Close();
                }
                ++videoNumber;
                paths.RemoveAt(0);
            }
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
                            itemList[j].Frame = itemList[i].Frame;
                        }
                    }
                }
            }

        }
    }
}
