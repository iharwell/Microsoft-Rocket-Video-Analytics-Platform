// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using LineDetector;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    public class LineCrossingProcessor : IProcessor
    {
        private readonly Detector _detector;

        public LineCrossingProcessor(string lineFile, int samplingFactor, double resFactor, ISet<string> categories, Color bbColor, bool display = false)
        {
            Categories = categories;
            BoundingBoxColor = bbColor;

            _detector = new Detector(samplingFactor, resFactor, lineFile, display);
            var lines = _detector._multiLaneDetector.getAllLines();
            LineSegments = new Dictionary<string, LineSegment>();

            for (int i = 0; i < lines.Count; i++)
            {
                LineSegments.Add(lines[i].key, lines[i].segments);
            }
            DisplayOutput = display;
        }

        public Color BoundingBoxColor { get; set; }
        public IDictionary<string, LineSegment> LineSegments { get; set; }
        public ISet<string> Categories { get; set; }
        public bool DisplayOutput
        {
            get => _detector.DISPLAY_BGS;
            set => _detector.DISPLAY_BGS = value;
        }

        public bool Run(IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage)
        {
            if (items is null || items.Count == 0)
            {
                _detector.UpdateLineResults(frame, items, this);
                if (items.Count > 0)
                {
                    return true;
                }
                return false;
            }

            _detector.UpdateLineResults(frame, items, this);

            for (int i = items.Count - 1; i >= 0; --i)
            {
                for (int j = items[i].ItemIDs.Count - 1; j > 0; --j)
                {
                    if (items[i].ItemIDs[j].SourceObject == this)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
