// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reflection;
using Utils.Config;
using Utils.Items;

namespace AML.Client
{
    public class AMLCaller
    {
        private static string Host;
        private static bool UseSSL;
        private static string Auth;
        private static string AksServiceName;
        private static string InputName = "Placeholder:0";
        private static string OutputName = "classifier/resnet_v1_50/predictions/Softmax:0";

        public AMLCaller(string host, bool useSSL, string auth, string aksServiceName)
        {
            try
            {
                Host = host;
                UseSSL = useSSL;
                Auth = auth;
                AksServiceName = aksServiceName;
            }
            catch (Exception e)
            {
                Console.WriteLine("AML init error:" + e);
            }
        }

        public static async Task<List<bool>> Run(int frameIndex, IList<IFramedItem> items, HashSet<string> category)
        {
            //could implement AML triggering criteria here, e.g., confidence

            if (items == null)
            {
                return null;
            }

            var client = new ScoringClient(Host, UseSSL ? 443 : 80, UseSSL, Auth, AksServiceName);
            List<bool> amlResult = new List<bool>();

            for (int itemIndex = 0; itemIndex < items.Count(); itemIndex++)
            {
                MemoryStream mStream = new MemoryStream();
                items[itemIndex].CroppedImageData(items[itemIndex].ItemIDs.Count - 1).WriteToStream(mStream);
                mStream.Position = 0;
                /*using ( Image image = Image.FromStream( new MemoryStream( items[itemIndex].CroppedImageData( items[itemIndex].ItemIDs.Count - 1 ) ) ) )
                {
                    image.Save(mStream, ImageFormat.Png);
                    mStream.Position = 0;
                }*/

                using (mStream)
                {
                    IScoringRequest request = new ImageRequest(InputName, mStream);
                    var stopWatch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        var result = await client.ScoreAsync<float[,]>(request, output_name: OutputName);
                        var latency = stopWatch.Elapsed;
                        for (int i = 0; i < result.GetLength(0); i++)
                        {
                            Console.WriteLine($"Latency: {latency}");
                            Console.WriteLine($"Batch {i}:");
                            var length = result.GetLength(1);
                            var results = new Dictionary<int, float>();
                            for (int j = 0; j < length; j++)
                            {
                                results.Add(j, result[i, j]);
                            }

                            foreach (var kvp in results.Where(x => x.Value > 0.001).OrderByDescending(x => x.Value).Take(5))
                            {
                                Console.WriteLine(
                                    $"    {GetLabel(kvp.Key)} {kvp.Value * 100}%");
                                char[] delimiterChars = { ' ', ',' };
                                foreach (var key in GetLabel(kvp.Key).Split(delimiterChars))
                                {
                                    if (category.Count == 0 || category.Contains(key))
                                    {
                                        amlResult.Add(true);

                                        // output AML results
                                        string blobName_AML = $@"frame-{frameIndex}-zAML-{key}-{kvp.Value}.jpg";
                                        string fileName_AML = @OutputFolder.OutputFolderAML + blobName_AML;
                                        var cropped = items[itemIndex].CroppedImageData(items[itemIndex].ItemIDs.Count - 1);
                                        Utils.Utils.WriteAllBytes(fileName_AML, cropped);
                                        Utils.Utils.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_AML, cropped);

                                        goto CheckNextItem;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("error:" + e);
                        return null;
                    }
                }
CheckNextItem:;
            }

            return amlResult;
        }

        private static Dictionary<int, string> _classes;

        private static string GetLabel(int classId)
        {
            if (_classes == null)
            {
                var assembly = typeof(AMLCaller).GetTypeInfo().Assembly;
                var result = assembly.GetManifestResourceStream("AML.Client.imagenet-classes.json");

                var streamReader = new StreamReader(result);
                var classesJson = streamReader.ReadToEnd();

                _classes = JsonConvert.DeserializeObject<Dictionary<int, string>>(classesJson);
            }

            return _classes[classId];
        }
    }
}
