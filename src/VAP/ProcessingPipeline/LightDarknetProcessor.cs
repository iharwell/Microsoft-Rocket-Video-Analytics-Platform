// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    /// <summary>
    ///   A lightweight Darknet Yolo DNN stage designed to work with a line triggering system.
    /// </summary>
    public class LightDarknetProcessor : IProcessor
    {
        private readonly DarknetDetector.LineTriggeredDNNDarknet _darknet;


        public LightDarknetProcessor(List<(string key, LineSegment coordinates)> lines)
            : this(lines, null, Color.Pink, false)
        { }

        public LightDarknetProcessor(List<(string key, LineSegment coordinates)> lines, ISet<string> categories, Color color, bool display)
        {
            _darknet = new DarknetDetector.LineTriggeredDNNDarknet(lines);
            LineSegments = new Dictionary<string, LineSegment>();
            for (int i = 0; i < lines.Count; i++)
            {
                LineSegments.Add(lines[i].key, lines[i].coordinates);
            }
            if (categories == null)
            {
                Categories = new HashSet<string>();
            }
            else
            {
                Categories = categories;
            }
            BoundingBoxColor = color;
            DisplayOutput = display;

        }
        public LightDarknetProcessor(IDictionary<string, LineSegment> lines, ISet<string> categories, Color color, bool display)
        {
            List<(string key, LineSegment coordinates)> llist = new List<(string key, LineSegment coordinates)>();
            foreach (var entry in lines)
            {
                llist.Add((entry.Key, entry.Value));
            }

            _darknet = new DarknetDetector.LineTriggeredDNNDarknet(llist);
            LineSegments = lines;
            if (categories == null)
            {
                Categories = new HashSet<string>();
            }
            else
            {
                Categories = categories;
            }
            BoundingBoxColor = color;
            DisplayOutput = display;

        }

        public IIndexChooser IndexChooser
        {
            get => _darknet.IndexChooser;
            set => _darknet.IndexChooser = value;
        }

        public Color BoundingBoxColor { get; set; }
        public IDictionary<string, LineSegment> LineSegments { get; set; }
        public ISet<string> Categories { get; set; }
        public bool DisplayOutput
        {
            get => _darknet.DisplayFrame;
            set => _darknet.DisplayFrame = value;
        }

        public bool Run(IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();

            List<(string key, LineSegment coordinates)> lines = new List<(string key, LineSegment coordinates)>();
            foreach (var entry in LineSegments)
            {
                lines.Add((entry.Key, entry.Value));
                counts.Add(entry.Key, 0);
            }

            var trigItems = Utils.Utils.GetItemsForFurtherProcessing(items);

            foreach (var trigFramedItem in trigItems)
            {
                var itemIDs = trigFramedItem.ItemIDs;
                for (int j = 0; j < itemIDs.Count; j++)
                {
                    if (object.ReferenceEquals(itemIDs[j].SourceObject, previousStage))
                    {
                        Debug.Assert(itemIDs[j] is ILineTriggeredItemID);
                        ILineTriggeredItemID ltid = itemIDs[j] as ILineTriggeredItemID;

                        if (counts.ContainsKey(ltid.TriggerLine))
                        {
                            counts[ltid.TriggerLine] = counts[ltid.TriggerLine] + 1;
                        }
                        else
                        {
                            counts[ltid.TriggerLine] = 1;
                        }
                    }
                }
            }

            //var res2 = _darknet.RunOld(frame, counts, lines, Categories, items, this);
            var res = _darknet.Run(frame, counts, lines, Categories, items, this);

            /*if((res.Count>0 && res.Last().ItemIDs.Last().SourceObject == this) || (res2.Count > 0 && res2.Last().ItemIDs.Last().SourceObject == this))
            {
                Console.WriteLine("\tItem Found");
            }*/

            for (int i = res.Count - 1; i > 0; --i)
            {
                if (res[i].ItemIDs[res[i].ItemIDs.Count - 1].SourceObject == this)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
