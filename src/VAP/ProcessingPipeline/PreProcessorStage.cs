// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using OpenCvSharp;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    public class PreProcessorStage : IProcessor
    {
        public PreProcessorStage()
        {
            detector = new BGSObjectDetector.BGSObjectDetector();

        }
        BGSObjectDetector.BGSObjectDetector detector { get; set; }
        public Color BoundingBoxColor { get; set; }
        public IDictionary<string, LineSegment> LineSegments { get; set; }
        public ISet<string> Categories { get; set; }
        public int SamplingFactor { get; set; }
        public double ResolutionFactor { get; set; }

        public bool DisplayOutput { get; set; }

        public bool Run(IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage)
        {
            frame.FrameData = FramePreProcessor.PreProcessor.returnFrame(frame.FrameData, frame.FrameIndex, SamplingFactor, ResolutionFactor, DisplayOutput);

            if (frame.FrameData == null)
            {
                return false;
            }

            var bgsItems = detector.DetectObjects(frame.TimeStamp, frame.FrameData, frame.FrameIndex, out Mat fg, this);
            frame.ForegroundMask = fg;
            FramedItem.MergeIntoFramedItemList(bgsItems, ref items);
            return bgsItems != null && bgsItems.Count > 0;
        }
    }
}
