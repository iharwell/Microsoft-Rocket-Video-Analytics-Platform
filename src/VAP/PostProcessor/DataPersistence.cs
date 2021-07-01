// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Model;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using Utils.Config;
using Utils.Items;

namespace PostProcessor
{
    public enum Position
    {
        Right,
        Left,
        Up,
        Down,
        Unknown
    }

    public class DataPersistence
    {
        //string blobUri_BGS = null;
        private static readonly AzureBlobProcessor s_blobProcessor = new();

        // force precise initialization
        static DataPersistence() { }

        public static void PersistResult(string dbCollectionName, string videoUrl, int cameraID, int frameIndex, IList<IFramedItem> detectionResult, Position[] objDir, string yOLOCONFIG, string yOLOCONFIG_HEAVY,
            string azureContainerName)
        {
            if (detectionResult != null && detectionResult.Count != 0)
            {
                foreach (IFramedItem it in detectionResult)
                {
                    var fileList = Directory.GetFiles(@OutputFolder.OutputFolderAll, $"frame-{frameIndex}*");
                    string blobName = Path.GetFileName(fileList[^1]);
                    //string blobName = it.IdentificationMethod == "Cheap" ? $@"frame-{frameIndex}-Cheap-{it.Confidence}.jpg" : $@"frame-{frameIndex}-Heavy-{it.Confidence}.jpg";
                    string blobUri = SendDataToCloud(azureContainerName, blobName, @OutputFolder.OutputFolderAll + blobName);
                    string serializedResult = SerializeDetectionResult(videoUrl, cameraID, frameIndex, it, objDir, blobUri, yOLOCONFIG, yOLOCONFIG_HEAVY);
                    WriteDB(dbCollectionName, serializedResult);
                }
            }
        }

        public static string SendDataToCloud(string containerName, string blobName, string sourceFile)
        {
            return AzureBlobProcessor.UploadFileAsync(containerName, blobName, sourceFile).GetAwaiter().GetResult();
        }

        private static string SerializeDetectionResult(string videoUrl, int cameraID, int frameIndex, IFramedItem item, Position[] objDir, string imageUri, string YOLOCONFIG, string YOLOCONFIG_HEAVY)
        {
            throw new NotImplementedException();
            Model.Consolidation detectionConsolidation = new Model.Consolidation
            {
                Key = Guid.NewGuid().ToString(),
                CameraID = cameraID,
                Frame = frameIndex,

                /*
                ObjID = item.ObjectID;
                ObjName = item.ObjName;
                Bbox = new int[] { item.X, item.Y, item.Height, item.Width };
                Prob = item.Confidence;
                ObjDir = objDir[0].ToString() + objDir[1].ToString();
                ImageUri = new Uri(imageUri);*/

                VideoInput = videoUrl,
                YoloCheap = YOLOCONFIG,
                YoloHeavy = YOLOCONFIG_HEAVY,
                Time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff")
            };

            //Create a stream to serialize the object to.  
            MemoryStream ms = new MemoryStream();

            // Serializer the User object to the stream.  
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Model.Consolidation));
            ser.WriteObject(ms, detectionConsolidation);
            byte[] json = ms.ToArray();
            ms.Close();
            return System.Text.Encoding.UTF8.GetString(json, 0, json.Length);
        }

        private static int WriteDB(string collectionName, string content)
        {
            //var createCltResult = Client.CreateCollection().Result;
            var createDocResult = DBClient.CreateDocument(collectionName, content).Result;
            return (int)createDocResult;
        }
    }
}
