// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Config;
using DNNDetector.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Utils.Config;
using Utils.Items;
using Wrapper.ORT;

namespace DNNDetector
{
    //Todo: merge it with LineTriggeredDNNYolo
    public class LineTriggeredDNNORTYolo
    {
        Dictionary<string, int> counts_prev = new Dictionary<string, int>();

        FrameDNNOnnxYolo frameDNNOnnxYolo;
        FrameBuffer frameBufferLtDNNOnnxYolo;

        public LineTriggeredDNNORTYolo(List<Tuple<string, int[]>> lines, string modelName)
        {
            frameBufferLtDNNOnnxYolo = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            frameDNNOnnxYolo = new FrameDNNOnnxYolo(lines, modelName, DNNMode.LT);

            Utils.Utils.cleanFolder(@OutputFolder.OutputFolderLtDNN);
            Utils.Utils.cleanFolder(@OutputFolder.OutputFolderFrameDNNONNX);
        }

        public IList<IFramedItem> Run(Mat frame, int frameIndex, Dictionary<string, int> counts, List<Tuple<string, int[]>> lines, Dictionary<string, int> category, ref long teleCountsCheapDNN, IList<IFramedItem> items,bool savePictures = false)
        {
            // buffer frame
            frameBufferLtDNNOnnxYolo.Buffer(frame);

            if (counts_prev.Count != 0)
            {
                foreach (string lane in counts.Keys)
                {
                    int diff = Math.Abs(counts[lane] - counts_prev[lane]);
                    if (diff > 0) //object detected by BGS
                    {
                        if (frameIndex >= DNNConfig.FRAME_SEARCH_RANGE)
                        {
                            // call onnx cheap model for crosscheck
                            int lineID = Array.IndexOf(counts.Keys.ToArray(), lane);
                            Mat[] frameBufferArray = frameBufferLtDNNOnnxYolo.ToArray();
                            int frameIndexOnnxYolo = frameIndex - 1;
                            IList<IFramedItem> analyzedTrackingItems = null;

                            while (frameIndex - frameIndexOnnxYolo < DNNConfig.FRAME_SEARCH_RANGE)
                            {
                                Console.WriteLine("** Calling DNN on " + (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexOnnxYolo)));
                                Mat frameOnnx = frameBufferArray[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexOnnxYolo)];

                                analyzedTrackingItems = frameDNNOnnxYolo.Run(frameOnnx, (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexOnnxYolo)), category, System.Drawing.Color.Pink, lineID, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE);
                                teleCountsCheapDNN++;
                                // object detected by cheap model
                                if (analyzedTrackingItems != null)
                                {
                                    foreach (IFramedItem frameditem in analyzedTrackingItems)
                                    {
                                        IItemID item = frameditem.ItemIDs.Last();
                                        Rectangle bounds = item.BoundingBox;
                                        ILineTriggeredItemID item2;
                                        if( item is ILineTriggeredItemID ltitem )
                                        {
                                            item2 = ltitem;
                                        }
                                        else
                                        {
                                            item2 = new LineTriggeredItemID(bounds, item.ObjectID, item.ObjName, item.Confidence, item.TrackID, nameof(FrameDNNOnnxYolo) );
                                        }
                                        item2.TriggerLine = lane;
                                        item2.TriggerLineID = lineID;

                                        IFramedItem framedItem;

                                        if ( items.Count == 0 || ( items.Count == 1 && items[0].Similarity( bounds ) > 0 ) )
                                        {
                                            items[0].ItemIDs.Add( item2 );
                                            framedItem = items[0];
                                        }
                                        else if ( items.Count == 1 )
                                        {
                                            framedItem = new FramedItem( items[0].Frame, item2);
                                            items.Add( framedItem );
                                        }
                                        else
                                        {
                                            int bestIndex = 0;
                                            double bestSimilarity = items[0].Similarity(bounds);
                                            for ( int i = 1; i < items.Count; i++ )
                                            {
                                                double sim = items[i].Similarity(bounds);
                                                if ( sim > bestSimilarity )
                                                {
                                                    bestIndex = i;
                                                    bestSimilarity = sim;
                                                }
                                            }
                                            if ( bestSimilarity > 0 )
                                            {
                                                items[bestIndex].ItemIDs.Add( item2 );
                                                framedItem = items[bestIndex];
                                            }
                                            else
                                            {
                                                framedItem = new FramedItem( items[0].Frame, item2);
                                                items.Add( framedItem );
                                            }
                                        }
                                        // output cheap onnx results
                                        if (savePictures)
                                        {
                                            string blobName_Cheap = $@"frame-{frameIndex}-DNN-{item.Confidence}.jpg";
                                            string fileName_Cheap = @OutputFolder.OutputFolderLtDNN + blobName_Cheap;
                                            var taggedImage = framedItem.TaggedImageData(framedItem.ItemIDs.Count-1, System.Drawing.Color.Pink);
                                            Utils.Utils.WriteAllBytes(fileName_Cheap, taggedImage );
                                            Utils.Utils.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_Cheap, taggedImage );
                                        }
                                    }
                                    updateCount(counts);
                                    return items;
                                }
                                frameIndexOnnxYolo--;
                            }
                        }
                    }
                }
            }
            updateCount(counts);
            return null;
        }

        void updateCount(Dictionary<string, int> counts)
        {
            foreach (string dir in counts.Keys)
            {
                counts_prev[dir] = counts[dir];
            }
        }
    }
}
