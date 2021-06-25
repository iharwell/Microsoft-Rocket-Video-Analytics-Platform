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
    /// <summary>
    ///   A preprocessor stage that performs background isolation as well as any scaling needed.
    /// </summary>
    public class PreProcessorStage : IProcessor
    {
        /// <summary>
        /// Creates a preprocessor stage with default parameters.
        /// </summary>
        public PreProcessorStage()
        {
            Detector = new BGSObjectDetector.BGSObjectDetector();
            BoundingBoxColor = Color.Gray;
            SamplingFactor = 1;
            ResolutionFactor = 1;
        }

        /// <summary>
        ///   Creates a preprocessor stage with the provided parameters.
        /// </summary>
        /// <param name="bboxColor">
        ///   The color of the bounding box used to tag a positive ID.
        /// </param>
        /// <param name="samplingFactor">
        ///   The sampling rate factor used by the pipeline. A value of 1 means that all frames are
        ///   used, 2 means that every other frame is used, etc.
        /// </param>
        /// <param name="resolutionFactor">
        ///   The resolution scaling factor used by the pipeline. A value of 1 means that no scaling
        ///   is performed, a value of 0.5 scales the width and height of the frame to half their
        ///   initial size, etc.
        /// </param>
        public PreProcessorStage(Color bboxColor, int samplingFactor, double resolutionFactor)
        {
            Detector = new BGSObjectDetector.BGSObjectDetector();
            BoundingBoxColor = bboxColor;
            SamplingFactor = samplingFactor;
            ResolutionFactor = resolutionFactor;
        }

        private BGSObjectDetector.BGSObjectDetector Detector { get; set; }

        /// <inheritdoc/>
        public Color BoundingBoxColor { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, LineSegment> LineSegments { get; set; }

        /// <inheritdoc/>
        public ISet<string> Categories { get; set; }

        /// <inheritdoc/>
        public int SamplingFactor { get; set; }

        /// <inheritdoc/>
        public double ResolutionFactor { get; set; }

        /// <inheritdoc/>
        public bool DisplayOutput { get; set; }

        /// <inheritdoc/>
        public bool Run(IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage)
        {
            //frame.FrameData = FramePreProcessor.PreProcessor.ReturnFrame(frame.FrameData, frame.FrameIndex, SamplingFactor, ResolutionFactor, DisplayOutput);

            if (frame.FrameData == null)
            {
                return false;
            }

            var bgsItems = Detector.DetectObjects(frame.TimeStamp, frame, this);
            FramedItem.MergeIntoFramedItemList(bgsItems, ref items);
            return bgsItems != null && bgsItems.Count > 0;
        }
    }
}
