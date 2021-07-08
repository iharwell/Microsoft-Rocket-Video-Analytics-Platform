// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarknetDetector;
using OpenCvSharp;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    public class SimpleDNNProcessor : IProcessor
    {
        public SimpleDNNDarknet _darknet;

        public SimpleDNNProcessor(SimpleDNNDarknet darknet)
        {
            _darknet = darknet;
        }

        public Color BoundingBoxColor { get; set; }
        public IDictionary<string, LineSegment> LineSegments { get; set; }
        public ISet<string> Categories { get; set; }
        public bool DisplayOutput { get; set; }

        public bool Run(IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage)
        {
            int prevResults = items.Count;
            items = _darknet.Run(frame, Categories, items, this);

            if (DisplayOutput && (frame.FrameIndex & 1) == 0)
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

        private static Rect ToRect(IItemID id)
        {
            return new Rect(id.BoundingBox.X, id.BoundingBox.Y, id.BoundingBox.Width, id.BoundingBox.Height);
        }
    }
}
