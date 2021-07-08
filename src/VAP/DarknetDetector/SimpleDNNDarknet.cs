// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Utils.Items;

namespace DarknetDetector
{
    public class SimpleDNNDarknet
    {
        private IDNNAnalyzer _analyzer;

        public SimpleDNNDarknet(IDNNAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public double MergeThreshold { get; set; }

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
    }
}
