// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Utils.Items;

namespace DarknetDetector
{
    [DataContract]
    public class SimpleDNNDarknet : IDisposable
    {
        [DataMember]
        private IDNNAnalyzer _analyzer;
        private bool _disposedValue;

        public SimpleDNNDarknet(IDNNAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        [DataMember]
        public double MergeThreshold { get; set; }

        [DataMember]
        public double NameBoost { get; set; }

        public IList<IFramedItem> Run(IFrame frame,
                                      ISet<string> category,
                                      IList<IFramedItem> items,
                                      object sourceObject)
        {
            var rawItems = _analyzer.Analyze(frame.FrameData, category, sourceObject);
            foreach (var id in rawItems)
            {
                IFramedItem fi = new FramedItem(frame, id);
                items.Add(fi);
                /*if (id.InsertIntoFramedItemList(items, out var framedItem, frame.FrameIndex))
                {
                    framedItem.Frame = frame;
                }*/
            }
            return items;
        }

        ~SimpleDNNDarknet() => Dispose(false);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if(_analyzer is IDisposable d)
                    {
                        d.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SimpleDNNDarknet()
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
    }
}
