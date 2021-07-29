// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DarknetDetector;
using OpenCvSharp;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    [DataContract]
    [KnownType(typeof(Dictionary<string, LineSegment>))]
    [KnownType(typeof(HashSet<string>))]
    public class SimpleDNNProcessor : IProcessor, IDisposable
    {
        [DataMember]
        private IDNNAnalyzer _analyzer;
        private bool _disposedValue;

        public SimpleDNNProcessor(IDNNAnalyzer analyzer)
        {
            _analyzer = analyzer;

            ExcludeCategories = new();

            IncludeCategories = new();
        }

        ~SimpleDNNProcessor() => Dispose(false);

        [DataMember]
        public Color BoundingBoxColor { get; set; }
        [DataMember]
        public HashSet<string> IncludeCategories { get; set; }
        /// <inheritdoc/>
        [DataMember]
        public HashSet<string> ExcludeCategories { get; set; }
        [DataMember]
        public bool DisplayOutput { get; set; }
        [DataMember]
        public IDictionary<string, LineSegment> LineSegments { get; set; }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool Run(IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage)
        {
            int prevResults = items.Count;
            items = RunAnalyzer(frame, IncludeCategories, items, this);



            if (DisplayOutput && (frame.FrameIndex & 3) == 0)
            {
                Mat output = frame.FrameData.Clone();
                var col = new Scalar(BoundingBoxColor.B, BoundingBoxColor.G, BoundingBoxColor.R);
                var white = new Scalar(255, 255, 255);
                for (int i = 0; i < items.Count; i++)
                {
                    var fi = items[i];
                    if (fi.Frame.FrameIndex != frame.FrameIndex)
                    {
                        continue;
                    }
                    for (int j = 0; j < fi.ItemIDs.Count; j++)
                    {
                        var it = fi.ItemIDs[j];
                        if (it.SourceObject == this)
                        {
                            output.Rectangle(ToRect(it), col, 2);
                        }
                    }
                }
                foreach (var entry in LineSegments)
                {
                    output.Line(entry.Value.P1.X, entry.Value.P1.Y, entry.Value.P2.X, entry.Value.P2.Y, white, 3);
                }
                Cv2.ImShow("Simple DNN Results", output);
                output.Dispose();
            }
            return items.Count != prevResults;

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (_analyzer is IDisposable d)
                {
                    d.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        private static Rect ToRect(IItemID id)
        {
            return new Rect(id.BoundingBox.X, id.BoundingBox.Y, id.BoundingBox.Width, id.BoundingBox.Height);
        }

        private IList<IFramedItem> RunAnalyzer(IFrame frame,
                                                               ISet<string> category,
                                               IList<IFramedItem> items,
                                               object sourceObject)
        {
            var rawItems = _analyzer.Analyze(frame.FrameData, category, sourceObject);
            foreach (var id in rawItems)
            {
                if (KeepItem(id))
                {
                    IFramedItem fi = new FramedItem(frame, id);
                    items.Add(fi);
                }
                /*if (id.InsertIntoFramedItemList(items, out var framedItem, frame.FrameIndex))
                {
                    framedItem.Frame = frame;
                }*/
            }
            return items;
        }

        private bool KeepItem(IItemID id)
        {
            if(IncludeCategories != null && IncludeCategories.Count>0)
            {
                if (id.ObjName != null && id.ObjName.Length > 0)
                {
                    return IncludeCategories.Contains(id.ObjName);
                }
                return true;
            }
            else if(ExcludeCategories!=null && ExcludeCategories.Count>0)
            {
                if (id.ObjName != null && id.ObjName.Length > 0)
                {
                    return !ExcludeCategories.Contains(id.ObjName);
                }
                return true;
            }
            return true;
        }
        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SimpleDNNProcessor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
    }
}
