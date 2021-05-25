// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

﻿using DNNDetector.Config;
using DNNDetector.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Utils;
using Utils.Config;
using Utils.Items;
using Wrapper.Yolo;
using Wrapper.Yolo.Model;

namespace DarknetDetector
{
    public class CascadedDNNDarknet
    {
        static string YOLOCONFIG = "YoloV3Coco"; // "cheap" yolo config folder name
        FrameDNNDarknet frameDNNYolo;
        FrameBuffer frameBufferCcDNN;

        public CascadedDNNDarknet(double rFactor)
        {
            frameBufferCcDNN = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.CC, rFactor);
        }

        public CascadedDNNDarknet(List<(string key, LineSegment coordinates)> lines)
        {
            frameBufferCcDNN = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            frameDNNYolo = new FrameDNNDarknet(YOLOCONFIG, DNNMode.CC, lines);
        }

        public IList<IFramedItem> Run(Mat frame, int frameIndex, IList<IFramedItem> ltDNNItemList, List<(string key, LineSegment coordinates)> lines, HashSet<string> category)
        {
            if (ltDNNItemList == null)
            {
                return null;
            }

            for ( int i = 0; i < ltDNNItemList.Count; i++ )
            {
                IFramedItem ltDNNItem = ltDNNItemList[i];
                var ltDNNID = ltDNNItem.ItemIDs.Last();
                if ( ltDNNID.Confidence >= DNNConfig.CONFIDENCE_THRESHOLD || !( ltDNNID is LineTriggeredItemID trigID && trigID.TriggerLine!=null ) )
                {
                    continue;
                }
                else
                {
                    Debug.Assert( ltDNNID is LineTriggeredItemID );
                    List<YoloTrackingItem> analyzedTrackingItems = null;
                    byte[] imgByte = ltDNNItem.Frame.FrameData;
                    int realFrameIndex = ltDNNItem.Frame.FrameIndex;

                    Console.WriteLine("** Calling Heavy");

                    if ( ltDNNID is LineTriggeredItemID lineTriggered )
                    {
                        frameDNNYolo.SetTrackingPoint( lines[lineTriggered.TriggerLineID].coordinates.MidPoint ); //only needs to check the last line in each row
                        analyzedTrackingItems = frameDNNYolo.Detect(imgByte, category, lineTriggered.TriggerLineID, System.Drawing.Brushes.Red, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL, realFrameIndex );
                    }
                    else
                    {
                        return frameDNNYolo.Run( imgByte, realFrameIndex, lines, category, System.Drawing.Brushes.Red );
                    }

                    // object detected by heavy YOLO
                    if (analyzedTrackingItems != null)
                    {
                        foreach (YoloTrackingItem yoloTrackingItem in analyzedTrackingItems)
                        {
                            Rectangle bounds = new Rectangle( yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height );
                            ItemID itemID = new ItemID( bounds, yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, yoloTrackingItem.Index, nameof( CascadedDNNDarknet) );
                            ltDNNItem.ItemIDs.Add( itemID );
                            // output heavy YOLO results
                            string blobName_Heavy = $@"frame-{realFrameIndex}-Heavy-{yoloTrackingItem.Confidence}.jpg";
                            string fileName_Heavy = @OutputFolder.OutputFolderCcDNN + blobName_Heavy;
                            var imgData = ltDNNItem.TaggedImageData(ltDNNItem.ItemIDs.Count - 1, Brushes.Red );
                            File.WriteAllBytes(fileName_Heavy, imgData );
                            File.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_Heavy, imgData );

                        }
                        return ltDNNItemList; // if we only return the closest object detected by heavy model
                    }
                    else
                    {
                        Console.WriteLine("**Not detected by Heavy");
                    }
                }
            }

            return ltDNNItemList;
        }

        Item Item(YoloTrackingItem yoloTrackingItem)
        {
            Item item = new Item(yoloTrackingItem.X, yoloTrackingItem.Y, yoloTrackingItem.Width, yoloTrackingItem.Height,
                yoloTrackingItem.ObjId, yoloTrackingItem.Type, yoloTrackingItem.Confidence, 0, "");

            item.TrackID = yoloTrackingItem.TrackId;
            item.Index = yoloTrackingItem.Index;
            item.TaggedImageData = yoloTrackingItem.TaggedImageData;
            item.CroppedImageData = yoloTrackingItem.CroppedImageData;

            return item;
        }
    }
}
