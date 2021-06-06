// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Config;
using DNNDetector.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using Utils.Config;
using Utils.Items;
using Wrapper.ORT;
using System.Linq;
using System.Drawing;

namespace DNNDetector
{
    public class CascadedDNNORTYolo
    {
        private FrameDNNOnnxYolo frameDNNOnnxYolo;

        public CascadedDNNORTYolo(List<Tuple<string, int[]>> lines, string modelName)
        {
            frameDNNOnnxYolo = new FrameDNNOnnxYolo(lines, modelName, DNNMode.CC);

            Utils.Utils.cleanFolder(@OutputFolder.OutputFolderCcDNN);
        }

        public IList<IFramedItem> Run(int frameIndex, IList<IFramedItem> ltDNNItemList, List<Tuple<string, int[]>> lines, Dictionary<string, int> category, ref long teleCountsHeavyDNN, bool savePictures = false)
        {
            if (ltDNNItemList == null)
            {
                return null;
            }

            IList<IFramedItem> ccDNNItem = new List<IFramedItem>();

            foreach ( IFramedItem ltDNNItem in ltDNNItemList)
            {
                if (ltDNNItem.ItemIDs.Last().Confidence >= DNNConfig.CONFIDENCE_THRESHOLD)
                {
                    ccDNNItem.Add(ltDNNItem);
                    continue;
                }
                else
                {
                    IList<IFramedItem> analyzedTrackingItems = null;
                    ILineTriggeredItemID ltID = (from IItemID id in ltDNNItem.ItemIDs where id is ILineTriggeredItemID select (ILineTriggeredItemID)id ).Last();
                    Console.WriteLine("** Calling Heavy DNN **");
                    analyzedTrackingItems = frameDNNOnnxYolo.Run(Cv2.ImDecode(ltDNNItem.Frame.FrameData, ImreadModes.Color), frameIndex, category, System.Drawing.Color.Yellow, ltID.TriggerLineID, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE);
                    teleCountsHeavyDNN++;

                    // object detected by heavy YOLO
                    if (analyzedTrackingItems != null)
                    {
                        foreach (IFramedItem frameditem in analyzedTrackingItems)
                        {
                            IItemID item = frameditem.ItemIDs.Last();
                            IFramedItem framed;
                            ILineTriggeredItemID item2;
                            if ( item is ILineTriggeredItemID ltitem )
                            {
                                item2 = ltitem;
                            }
                            else
                            {
                                item2 = new LineTriggeredItemID(item.BoundingBox, item.ObjectID, item.ObjName, item.Confidence, item.TrackID, nameof(CascadedDNNORTYolo) );
                                item2.TriggerLine = ltID.TriggerLine;
                                item2.TriggerLineID = ltID.TriggerLineID;
                            }


                            if ( ltDNNItemList.Count == 0 || ( ltDNNItemList.Count == 1 && ltDNNItemList[0].Similarity( item.BoundingBox ) > 0 ) )
                            {
                                ltDNNItemList[0].ItemIDs.Add( item2 );
                                framed = ltDNNItemList[0];
                            }
                            else if ( ltDNNItemList.Count == 1 )
                            {
                                framed = new FramedItem( ltDNNItemList[0].Frame, item2);
                                ltDNNItemList.Add( framed );
                            }
                            else
                            {
                                int bestIndex = 0;
                                double bestSimilarity = ltDNNItemList[0].Similarity(item.BoundingBox);
                                for ( int i = 1; i < ltDNNItemList.Count; i++ )
                                {
                                    double sim = ltDNNItemList[i].Similarity(item.BoundingBox);
                                    if ( sim > bestSimilarity )
                                    {
                                        bestIndex = i;
                                        bestSimilarity = sim;
                                    }
                                }
                                if ( bestSimilarity > 0 )
                                {
                                    ltDNNItemList[bestIndex].ItemIDs.Add( item2 );
                                    framed = ltDNNItemList[bestIndex];
                                }
                                else
                                {
                                    framed = new FramedItem( ltDNNItemList[0].Frame, item2);
                                    ltDNNItemList.Add( framed );
                                }
                            }

                            // output heavy YOLO results
                            if (savePictures)
                            {
                                string blobName_Heavy = $@"frame-{frameIndex}-Heavy-{item.Confidence}.jpg";
                                string fileName_Heavy = @OutputFolder.OutputFolderCcDNN + blobName_Heavy;
                                var taggedImageData = framed.TaggedImageData(framed.ItemIDs.Count-1, System.Drawing.Color.Yellow );
                                /*File.WriteAllBytes(fileName_Heavy,taggedImageData);
                                File.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_Heavy, taggedImageData);*/


                                Stream s = new FileStream(fileName_Heavy, FileMode.OpenOrCreate);
                                Stream s2 = new FileStream(@OutputFolder.OutputFolderAll + blobName_Heavy, FileMode.OpenOrCreate);
                                taggedImageData.WriteToStream( s, ".bmp" );
                                taggedImageData.WriteToStream( s2, ".bmp" );
                                s.Flush();
                                s2.Flush();
                                s.Close();
                                s2.Close();
                            }

                            return ccDNNItem; // if we only return the closest object detected by heavy model
                        }
                    }
                    else
                    {
                        Console.WriteLine("** Not detected by Heavy DNN **");
                    }
                }
            }

            return ccDNNItem;
        }
    }
}
