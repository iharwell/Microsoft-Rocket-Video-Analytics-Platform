// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

﻿using AML.Client;
using BGSObjectDetector;
using DarknetDetector;
using DNNDetector;
using DNNDetector.Config;
using DNNDetector.Model;
using LineDetector;
using MotionTracker;
using OpenCvSharp;
using PostProcessor;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using TFDetector;
using Utils;
using Utils.Items;

namespace VideoPipelineCore
{
    class Program
    {
        const int BUFFERSIZE = 90;

        static void Main(string[] args)
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
            int SAMPLING_FACTOR = int.Parse(args[2]);
            double RESOLUTION_FACTOR = double.Parse(args[3]);

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
            Utils.Utils.cleanFolderAll();

            //create pipeline components (initialization based on pplConfig)

            //-----FramedItem buffer for tracking item paths-----
            IList<IList<IFramedItem>> framedItemBuffer = new List<IList<IFramedItem>>(51);

            //-----Decoder-----
            Decoder.Decoder decoder = new Decoder.Decoder(videoUrl, loop);

            //-----Background Subtraction-based Detector-----
            BGSObjectDetector.BGSObjectDetector bgs = new BGSObjectDetector.BGSObjectDetector();

            //-----Line Detector-----
            Detector lineDetector = new Detector(SAMPLING_FACTOR, RESOLUTION_FACTOR, lineFile, displayBGSVideo);
            Dictionary<string, int> counts = null;
            Dictionary<string, bool> occupancy = null;
            List<(string key, LineSegment coordinates)> lines = lineDetector.multiLaneDetector.getAllLines();

            //-----LineTriggeredDNN (Darknet)-----
            LineTriggeredDNNDarknet ltDNNDarknet = null;
            IList<IFramedItem> ltDNNItemListDarknet = null;
            if (new int[] { 3, 4 }.Contains(pplConfig))
            {
                ltDNNDarknet = new LineTriggeredDNNDarknet(lines);
                ltDNNItemListDarknet = new List<IFramedItem>();
            }

            //-----LineTriggeredDNN (TensorFlow)-----
            LineTriggeredDNNTF ltDNNTF = null;
            IList<IFramedItem> ltDNNItemListTF = null;
            if (new int[] { 5,6 }.Contains(pplConfig))
            {
                ltDNNTF = new LineTriggeredDNNTF(lines);
                ltDNNItemListTF = new List<IFramedItem>();
            }

            //-----LineTriggeredDNN (ONNX)-----
            LineTriggeredDNNORTYolo ltDNNOnnx = null;
            IList<IFramedItem> ltDNNItemListOnnx = null;
            if (new int[] { 7 }.Contains(pplConfig))
            {
                ltDNNOnnx = new LineTriggeredDNNORTYolo(Utils.Utils.ConvertLines(lines), "yolov3tiny");
                ltDNNItemListOnnx = new List<IFramedItem>();
            }

            //-----CascadedDNN (Darknet)-----
            CascadedDNNDarknet ccDNNDarknet = null;
            IList<IFramedItem> ccDNNItemListDarknet = null;
            if (new int[] { 3 }.Contains(pplConfig))
            {
                ccDNNDarknet = new CascadedDNNDarknet(lines);
                ccDNNItemListDarknet = new List<IFramedItem>();
            }

            //-----CascadedDNN (ONNX)-----
            CascadedDNNORTYolo ccDNNOnnx = null;
            IList<IFramedItem> ccDNNItemListOnnx = null;
            if (new int[] { 7 }.Contains(pplConfig))
            {
                ccDNNOnnx = new CascadedDNNORTYolo(Utils.Utils.ConvertLines(lines), "yolov3");
                ccDNNItemListOnnx = new List<IFramedItem>();
            }

            //-----DNN on every frame (Darknet)-----
            FrameDNNDarknet frameDNNDarknet = null;
            IList<IFramedItem> frameDNNDarknetItemList = null;
            if (new int[] { 1 }.Contains(pplConfig))
            {
                frameDNNDarknet = new FrameDNNDarknet("YoloV3TinyCoco", Wrapper.Yolo.DNNMode.Frame, lines);
                frameDNNDarknetItemList = new List<IFramedItem>();
            }

            //-----DNN on every frame (TensorFlow)-----
            FrameDNNTF frameDNNTF = null;
            IList<IFramedItem> frameDNNTFItemList = null;
            if (new int[] { 2 }.Contains(pplConfig))
            {
                frameDNNTF = new FrameDNNTF(lines);
                frameDNNTFItemList = new List<IFramedItem>();
            }

            //-----DNN on every frame (ONNX)-----
            FrameDNNOnnxYolo frameDNNOnnxYolo = null;
            IList<IFramedItem> frameDNNONNXItemList = null;
            if (new int[] { 8 }.Contains(pplConfig))
            {
                frameDNNOnnxYolo = new FrameDNNOnnxYolo(Utils.Utils.ConvertLines(lines), "yolov3", Wrapper.ORT.DNNMode.Frame);
                frameDNNONNXItemList = new List<IFramedItem>();
            }

            //-----Call ML models deployed on Azure Machine Learning Workspace-----
            AMLCaller amlCaller = null;
            List<bool> amlConfirmed;
            if (new int[] { 6 }.Contains(pplConfig))
            {
                amlCaller = new AMLCaller(ConfigurationManager.AppSettings["AMLHost"],
                Convert.ToBoolean(ConfigurationManager.AppSettings["AMLSSL"]),
                ConfigurationManager.AppSettings["AMLAuthKey"],
                ConfigurationManager.AppSettings["AMLServiceID"]);
            }

            //-----Write to DB-----
            IList<IFramedItem> ItemList = null;
            IList<IItemPath> ItemPaths = new List<IItemPath>();

            int frameIndex = 0;
            int videoTotalFrame = 0;
            if (!isVideoStream)
                videoTotalFrame = decoder.getTotalFrameNum() - 1; //skip the last frame which could be wrongly encoded from vlc capture

            long teleCountsBGS = 0, teleCountsCheapDNN = 0, teleCountsHeavyDNN = 0;

            //-----Last minute prep-----
            DateTime videoTimeStamp;
            if( isVideoStream )
            {
                videoTimeStamp = DateTime.Now;
            }
            else
            {
                //TODO(iharwell): Find a portable option for pulling the "Media Created" property from a file for use here.
                // videoTimeStamp = decoder.getTimeStamp();

                videoTimeStamp = DateTime.Now;
            }

            double frameRate = decoder.getVideoFPS();

            //RUN PIPELINE 
            DateTime startTime = DateTime.Now;
            DateTime prevTime = DateTime.Now;
            while (true)
            {
                if (!loop)
                {
                    if (!isVideoStream && frameIndex >= videoTotalFrame)
                    {
                        break;
                    }
                }

                //decoder
                Mat frame = decoder.getNextFrame();

                
                //frame pre-processor
                frame = FramePreProcessor.PreProcessor.returnFrame(frame, frameIndex, SAMPLING_FACTOR, RESOLUTION_FACTOR, displayRawVideo);
                frameIndex++;
                if (frame == null) continue;
                //Console.WriteLine("Frame ID: " + frameIndex);


                //background subtractor
                Mat fgmask = null;
                IList<IFramedItem> foregroundBoxes = bgs.DetectObjects(DateTime.Now, frame, frameIndex, out fgmask);
                
                if ( foregroundBoxes != null && foregroundBoxes.Count > 0 )
                {
                    //Console.WriteLine( "Foreground boxes exist." );
                }

                //line detector
                if (new int[] { 0, 3, 4, 5, 6, 7 }.Contains(pplConfig))
                {
                    (counts, occupancy) = lineDetector.updateLineResults(frame, frameIndex, fgmask, foregroundBoxes);
                }


                //cheap DNN
                if (new int[] { 3, 4 }.Contains(pplConfig))
                {
                    ltDNNItemListDarknet = ltDNNDarknet.Run(frame, frameIndex, counts, lines, category, foregroundBoxes);
                    if ( ltDNNItemListDarknet != null && ltDNNItemListDarknet.Count > 0 )
                        ItemList = ltDNNItemListDarknet;
                }
                else if (new int[] { 5, 6 }.Contains(pplConfig))
                {
                    ltDNNItemListTF = ltDNNTF.Run(frame, frameIndex, counts, lines, category, foregroundBoxes);
                    ItemList = ltDNNItemListTF;
                }
                else if (new int[] { 7 }.Contains(pplConfig))
                {
                    ltDNNItemListOnnx = ltDNNOnnx.Run(frame, frameIndex, counts, Utils.Utils.ConvertLines(lines), Utils.Utils.CatHashSet2Dict(category), ref teleCountsCheapDNN, foregroundBoxes, true);
                    ItemList = ltDNNItemListOnnx;
                }


                //heavy DNN
                if (new int[] { 3 }.Contains(pplConfig))
                {
                    ccDNNItemListDarknet = ccDNNDarknet.Run(frame, frameIndex, ltDNNItemListDarknet, lines, category);
                    ItemList = ccDNNItemListDarknet;
                }
                else if (new int[] { 7 }.Contains(pplConfig))
                {
                    ccDNNItemListOnnx = ccDNNOnnx.Run(frameIndex, ItemList, Utils.Utils.ConvertLines(lines), Utils.Utils.CatHashSet2Dict(category), ref teleCountsHeavyDNN, true);
                    ItemList = ccDNNItemListOnnx;
                }


                //frameDNN with Darknet Yolo
                if (new int[] { 1 }.Contains(pplConfig))
                {
                    frameDNNDarknetItemList = frameDNNDarknet.Run(frame, frameIndex, lines, category, System.Drawing.Color.Pink);
                    ItemList = frameDNNDarknetItemList;
                }


                //frame DNN TF
                if (new int[] { 2 }.Contains(pplConfig))
                {
                    frameDNNTFItemList = frameDNNTF.Run(frame, frameIndex, category, System.Drawing.Color.Pink, 0.2);
                    ItemList = frameDNNTFItemList;
                }


                //frame DNN ONNX Yolo
                if (new int[] { 8 }.Contains(pplConfig))
                {
                    frameDNNONNXItemList = frameDNNOnnxYolo.Run(frame, frameIndex, Utils.Utils.CatHashSet2Dict(category), System.Drawing.Color.Pink, 0, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL, true);
                    ItemList = frameDNNONNXItemList;
                }


                //Azure Machine Learning
                if (new int[] { 6 }.Contains(pplConfig))
                {
                    amlConfirmed = AMLCaller.Run(frameIndex, ItemList, category).Result;
                }


                //DB Write
                if (new int[] { 4 }.Contains(pplConfig))
                {
                    Position[] dir = { Position.Unknown, Position.Unknown }; // direction detection is not included
                    DataPersistence.PersistResult("test", videoUrl, 0, frameIndex, ItemList, dir, "Cheap", "Heavy", // ArangoDB database
                                                            "test"); // Azure blob
                }

                //Merge IFrame items to conserve memory.
                CompressIFrames( ItemList );

                //display counts
                if (ItemList != null && ItemList.Count>0)
                {
                    Dictionary<string, string> kvpairs = new Dictionary<string, string>();
                    foreach (IFramedItem it in ItemList)
                    {
                        foreach ( IItemID ID in it.ItemIDs )
                        {
                            if ( ID is ILineTriggeredItemID ltID && ltID.TriggerLine!=null )
                            {
                                if ( !kvpairs.ContainsKey( ltID.TriggerLine ) )
                                    kvpairs.Add( ltID.TriggerLine, "1" );
                                break;
                            }
                        }
                        it.Frame.SourceName = videoUrl;
                        it.Frame.TimeStamp = videoTimeStamp.AddTicks( (long)( TimeSpan.TicksPerSecond * it.Frame.FrameIndex / frameRate ) );
                    }
                    FramePreProcessor.FrameDisplay.updateKVPairs(kvpairs);


                    // Currently requires the use of a detection line filter, as it generates too many ItemPaths without it.
                    if( ItemList.Last().ItemIDs.Last().IdentificationMethod.CompareTo( nameof(BGSObjectDetector) ) != 0 )
                    {
                        if ( ItemList.Last().ItemIDs.Last().IdentificationMethod.CompareTo( nameof( DetectionLine ) ) != 0 )
                        {
                            var path = MotionTracker.MotionTracker.GetPathFromIdAndBuffer( ItemList.Last(), framedItemBuffer, new PolyPredictor(), 0.3 );
                            ItemPaths.Add( path );
                        }
                    }

                    framedItemBuffer.Add( ItemList );
                    if ( framedItemBuffer.Count > BUFFERSIZE )
                    {
                        framedItemBuffer.RemoveAt( 0 );
                    }
                }


                //print out stats
                if ( ( frameIndex & 0xF ) == 0 )
                {
                    double fps = 1000 * (double)(1) / (DateTime.Now - prevTime).TotalMilliseconds;
                    double avgFps = 1000 * (long)frameIndex / (DateTime.Now - startTime).TotalMilliseconds;
                    Console.WriteLine( "{0} {1,-5} {2} {3,-5} {4} {5,-15} {6} {7,-10:N2} {8} {9,-10:N2}",
                                        "sFactor:", SAMPLING_FACTOR, "rFactor:", RESOLUTION_FACTOR, "FrameID:", frameIndex, "FPS:", fps, "avgFPS:", avgFps );
                }
                prevTime = DateTime.Now;
            }
            for ( int i = 0; i < ItemPaths.Count; i++ )
            {
                Console.WriteLine( "Item path length " + ( i + 1 ) + ": " + ItemPaths[i].FramedItems.Count );
            }
           Console.WriteLine("Done!");
        }

        private static void CompressIFrames( IList<IFramedItem> ItemList )
        {
            if( ItemList is null )
            {
                return;
            }

            for ( int i = 0; i < ItemList.Count; i++ )
            {
                int frameIndex = ItemList[i].Frame.FrameIndex;
                for ( int j = i + 1; j < ItemList.Count; j++ )
                {
                    if( ItemList[j].Frame.FrameIndex == frameIndex )
                    {
                        if( !object.ReferenceEquals(ItemList[j].Frame, ItemList[i].Frame ) )
                        {
                            ItemList[j].Frame = ItemList[i].Frame;
                        }
                    }
                }
            }

        }
    }
}
