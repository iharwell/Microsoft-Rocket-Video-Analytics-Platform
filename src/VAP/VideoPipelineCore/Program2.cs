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

namespace VideoPipelineCore
{
    internal class Program2
    {
        private const int BUFFERSIZE = 90;

        internal static void Main(string[] args)
        {
            //parse arguments
            if (args.Length < 4)
            {
                Console.WriteLine(args.Length);
                Console.WriteLine("Usage: <exe> <video url> <cfg file> <samplingFactor> <resolutionFactor> <category1> <category2> ...");
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
                videoUrl = @"..\..\..\..\..\..\media\" + args[0];
            }
            string lineFile = @"..\..\..\..\..\..\cfg\" + args[1];
            int samplingFactor = int.Parse(args[2]);
            double resolutionFactor = double.Parse(args[3]);

            HashSet<string> category = new HashSet<string>();
            for (int i = 4; i < args.Length; i++)
            {
                category.Add(args[i]);
            }

            //initialize pipeline settings
            int pplConfig = Convert.ToInt16(ConfigurationManager.AppSettings["PplConfig"]);
            bool loop = false;
            bool displayRawVideo = false;
            bool displayBGSVideo = false;
            Utils.Utils.CleanFolderAll();

            PolyPredictor polyPredictor = new PolyPredictor();
            IoUPredictor ioUPredictor = new IoUPredictor();

            //-----FramedItem buffer for tracking item paths-----
            IList<IList<IFramedItem>> framedItemBuffer = new List<IList<IFramedItem>>(51);

            //-----Decoder-----
            Decoder.Decoder2V decoder = new Decoder.Decoder2V(videoUrl, resolutionFactor, loop);

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

            LightDarknetProcessor lightDNProcessor = new LightDarknetProcessor(lcProcessor.LineSegments, category, Color.Pink, false);
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

            IList<IFramedItem> itemList = null;
            IList<IItemPath> itemPaths = new List<IItemPath>();
            //string lastStageName = pipeline.LastStage.GetType().Name;
            //string secondToLastStageName = pipeline[pipeline.Count-2].GetType().Name;
            object lastStage = pipeline.LastStage;
            object secondToLastStage = pipeline[^2];

            //RUN PIPELINE 
            DateTime startTime = DateTime.Now;
            DateTime prevTime = DateTime.Now;
            decoder.BeginReading();
            while (true)
            {
                if (!loop)
                {
                    if (!isVideoStream && frameIndex >= videoTotalFrame - 1)
                    {
                        break;
                    }
                }
                itemList = null;
                //decoder
                IFrame frame = new Frame
                {
                    FrameData = decoder.GetNextFrame()
                };
                if (frame.FrameData == null)
                {
                    continue;
                }
                frame.FrameIndex = frameIndex;
                frame.SourceName = args[0];
                frame.TimeStamp = startTime.AddSeconds((frameIndex * 1.0 / frameRate * TimeSpan.TicksPerSecond));

                itemList = pipeline.ProcessFrame(frame);

                if (frame == null) continue;


                if ((frameIndex & 0x3F) == 0)
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
                        it.Frame.SourceName = videoUrl;
                        it.Frame.TimeStamp = videoTimeStamp.AddTicks((long)(TimeSpan.TicksPerSecond * it.Frame.FrameIndex / frameRate));
                    }
                    FramePreProcessor.FrameDisplay.UpdateKVPairs(kvpairs);
                    object lastSource = itemList.Last().ItemIDs.Last().SourceObject;
                    // string lastMethod = ItemList.Last().ItemIDs.Last().IdentificationMethod;
                    // Currently requires the use of a detection line filter, as it generates too many ItemPaths without it.
                    if (object.ReferenceEquals(lastSource, lastStage) || object.ReferenceEquals(lastSource, secondToLastStage))
                    {
                        var path = MotionTracker.MotionTracker.GetPathFromIdAndBuffer(itemList.Last(), framedItemBuffer, polyPredictor, 0.3);
                        int prevCount;
                        int currentCount = path.FramedItems.Count;
                        do
                        {
                            prevCount = currentCount;
                            MotionTracker.MotionTracker.ExpandPathFromBuffer(path, framedItemBuffer, ioUPredictor, 0.3);
                            currentCount = path.FramedItems.Count;

                            if (currentCount > prevCount)
                            {
                                prevCount = currentCount;
                                MotionTracker.MotionTracker.ExpandPathFromBuffer(path, framedItemBuffer, polyPredictor, 0.3);
                                currentCount = path.FramedItems.Count;
                            }
                        } while (currentCount > prevCount)
                            ;
                        itemPaths.Add(path);
                    }

                    framedItemBuffer.Add(itemList);

                    if (framedItemBuffer.Count > BUFFERSIZE)
                    {
                        framedItemBuffer.RemoveAt(0);
                    }
                }


                //print out stats
                if ((frameIndex & 0xF) == 0)
                {
                    double fps = 1000 * (double)(1) / (DateTime.Now - prevTime).TotalMilliseconds;
                    double avgFps = 1000 * (long)frameIndex / (DateTime.Now - startTime).TotalMilliseconds;
                    Console.WriteLine("{0} {1,-5} {2} {3,-5} {4} {5,-15} {6} {7,-10:N2} {8} {9,-10:N2}",
                                        "sFactor:", samplingFactor, "rFactor:", resolutionFactor, "FrameID:", frameIndex, "FPS:", fps, "avgFPS:", avgFps);
                }
                ++frameIndex;
                prevTime = DateTime.Now;
            }
            for (int i = 0; i < itemPaths.Count; i++)
            {
                Console.WriteLine("Item path length " + (i + 1) + ": " + itemPaths[i].FramedItems.Count);
            }
            string output = "output";
            DataContractSerializer serializer = new DataContractSerializer(typeof(ItemPath));
            for (int i = 0; i < itemPaths.Count; i++)
            {
                if (itemPaths[i] is ItemPath path)
                {
                    string name = output + i;
                    using StreamWriter writer = new StreamWriter(name + ".xml");
                    serializer.WriteObject(writer.BaseStream, path);
                    writer.Flush();
                    writer.Close();
                }
            }
            Console.WriteLine("Done!");
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
