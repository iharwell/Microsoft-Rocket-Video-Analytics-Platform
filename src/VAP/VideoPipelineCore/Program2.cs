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
    class Program2
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
            bool displayRawVideo = true;
            bool displayBGSVideo = true;
            Utils.Utils.cleanFolderAll();

            PolyPredictor polyPredictor = new PolyPredictor();
            IoUPredictor ioUPredictor = new IoUPredictor();

            //-----FramedItem buffer for tracking item paths-----
            IList<IList<IFramedItem>> framedItemBuffer = new List<IList<IFramedItem>>(51);

            //-----Decoder-----
            Decoder.Decoder decoder = new Decoder.Decoder(videoUrl, loop);

            Pipeline pipeline = new Pipeline();
            PreProcessorStage bgsStage = new PreProcessorStage();
            bgsStage.SamplingFactor = SAMPLING_FACTOR;
            bgsStage.ResolutionFactor = RESOLUTION_FACTOR;
            bgsStage.BoundingBoxColor = Color.White;
            bgsStage.Categories = category;

            bgsStage.DisplayOutput = displayRawVideo;
            pipeline.AppendStage( bgsStage );

            LineCrossingProcessor lcProcessor = new LineCrossingProcessor( lineFile, SAMPLING_FACTOR, RESOLUTION_FACTOR, category, Color.CadetBlue );
            lcProcessor.DisplayOutput = displayBGSVideo;
            pipeline.AppendStage( lcProcessor );

            LightDarknetProcessor lightDNProcessor = new LightDarknetProcessor( lcProcessor.LineSegments, category, Color.Pink, false );
            pipeline.AppendStage( lightDNProcessor );

            HeavyDarknetProcessor heavyDNProcessor = new HeavyDarknetProcessor( lcProcessor.LineSegments, category, RESOLUTION_FACTOR, Color.Red, false );
            pipeline.AppendStage( heavyDNProcessor );

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
            int frameIndex = 0;
            int videoTotalFrame = decoder.getTotalFrameNum();

            IList<IFramedItem> ItemList = null;
            IList<IItemPath> ItemPaths = new List<IItemPath>();
            //string lastStageName = pipeline.LastStage.GetType().Name;
            //string secondToLastStageName = pipeline[pipeline.Count-2].GetType().Name;
            object lastStage = pipeline.LastStage;
            object secondToLastStage = pipeline[pipeline.Count-2];

            //RUN PIPELINE 
            DateTime startTime = DateTime.Now;
            DateTime prevTime = DateTime.Now;
            while (true)
            {
                if (!loop)
                {
                    if (!isVideoStream && frameIndex >= videoTotalFrame-1)
                    {
                        break;
                    }
                }
                ItemList = null;
                //decoder
                IFrame frame = new Frame();
                frame.FrameData = decoder.getNextFrame();
                if( frame.FrameData == null )
                {
                    continue;
                }
                frame.FrameIndex = frameIndex;
                frame.SourceName = args[0];
                frame.TimeStamp = startTime.AddSeconds( ( frameIndex * 1.0 / frameRate * TimeSpan.TicksPerSecond ) );

                ItemList = pipeline.ProcessFrame( frame );

                if (frame == null) continue;


                if ( ( frameIndex & 0x3F ) == 0 )
                {
                    GC.Collect();
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
                    object lastSource = ItemList.Last().ItemIDs.Last().SourceObject;
                    // string lastMethod = ItemList.Last().ItemIDs.Last().IdentificationMethod;
                    // Currently requires the use of a detection line filter, as it generates too many ItemPaths without it.
                    if ( object.ReferenceEquals( lastSource, lastStage ) || object.ReferenceEquals( lastSource, secondToLastStage ) )
                    {
                        var path = MotionTracker.MotionTracker.GetPathFromIdAndBuffer( ItemList.Last(), framedItemBuffer, polyPredictor, 0.3 );
                        int prevCount;
                        int currentCount = path.FramedItems.Count;
                        do
                        {
                            prevCount = currentCount;
                            MotionTracker.MotionTracker.ExpandPathFromBuffer( path, framedItemBuffer, ioUPredictor, 0.3 );
                            currentCount = path.FramedItems.Count;

                            if ( currentCount > prevCount )
                            {
                                prevCount = currentCount;
                                MotionTracker.MotionTracker.ExpandPathFromBuffer( path, framedItemBuffer, polyPredictor, 0.3 );
                                currentCount = path.FramedItems.Count;
                            }
                        } while ( currentCount > prevCount )
                            ;
                        ItemPaths.Add( path );
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
                ++frameIndex;
                prevTime = DateTime.Now;
            }
            for ( int i = 0; i < ItemPaths.Count; i++ )
            {
                Console.WriteLine( "Item path length " + ( i + 1 ) + ": " + ItemPaths[i].FramedItems.Count );
            }
            string output = "output";
            DataContractSerializer serializer = new DataContractSerializer(typeof(ItemPath) );
            for ( int i = 0; i < ItemPaths.Count; i++ )
            {
                if ( ItemPaths[i] is ItemPath path )
                {
                    string name = output+i;
                    using ( StreamWriter writer = new StreamWriter( name + ".xml" ) )
                    {
                        serializer.WriteObject( writer.BaseStream, path );
                        writer.Flush();
                        writer.Close();
                    }
                }
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
