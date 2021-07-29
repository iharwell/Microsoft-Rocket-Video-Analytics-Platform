// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Utils;
using Utils.Items;

namespace DarknetAAB
{
    [Serializable]
    public class Yolo4DNN : IDNNAnalyzer, IDisposable, ISerializable
    {
        private YoloWrapper _yolo;
        private bool _disposedValue;

        private string ConfigFile;
        private string WeightsFile;
        private int Gpu;

        public string Name { get; set; }

        public Yolo4DNN(string configFile, string weightsFile, int gpu)
        {
            _yolo = new YoloWrapper(configFile, weightsFile, gpu);
            ConfigFile = configFile;
            WeightsFile = weightsFile;
            Gpu = gpu;
        }
        public Yolo4DNN(string name, string configFile, string weightsFile, int gpu)
        {
            _yolo = new YoloWrapper(configFile, weightsFile, gpu);
            Name = name;
            ConfigFile = configFile;
            WeightsFile = weightsFile;
            Gpu = gpu;
        }
        public Yolo4DNN(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString(nameof(Name));
            ConfigFile = info.GetString(nameof(ConfigFile));
            WeightsFile = info.GetString(nameof(WeightsFile));
            Gpu = info.GetInt32(nameof(Gpu));
            _yolo = new YoloWrapper(ConfigFile, WeightsFile, Gpu);
        }
        public IEnumerable<IItemID> Analyze(Mat frameData, ISet<string> category, object sourceObject)
        {
            var results = _yolo.Detect(frameData);
            List<IItemID> items = new();
            if (results == null)
            {
                return items;
            }

            if (category == null || category.Count == 0)
            {
                for (int i = 0; i < results.Length; i++)
                {
                    items.Add(ConvertYoloItem(results[i], sourceObject));
                }
            }
            else
            {

                for (int i = 0; i < category.Count; i++)
                {
                    if (category.Contains(Coco.Names[results[i].obj_id]))
                    {
                        items.Add(ConvertYoloItem(results[i], sourceObject));
                    }
                }
            }
            return items;
        }

        ~Yolo4DNN() => Dispose(false);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                _yolo.Dispose();
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Yolo4DNN()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private IItemID ConvertYoloItem(bbox_t itembbox, object sourceObject)
        {
            return new ItemID()
            {
                BoundingBox = new System.Drawing.Rectangle((int)itembbox.x, (int)itembbox.y, (int)itembbox.w, (int)itembbox.h),
                Confidence = itembbox.prob,
                IdentificationMethod = nameof(Yolo4DNN),
                ObjectID = (int)itembbox.obj_id,
                TrackID = (int)itembbox.track_id,
                ObjName = Coco.Names[itembbox.obj_id],
                SourceObject = sourceObject
            };
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(ConfigFile), ConfigFile);
            info.AddValue(nameof(WeightsFile), WeightsFile);
            info.AddValue(nameof(Gpu), Gpu);
        }
    }
}
