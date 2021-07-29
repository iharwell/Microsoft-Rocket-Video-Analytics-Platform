// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text;
using LineDetector;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    /// <summary>
    ///   Takes bounding boxes found in a previous stage and determines whether any of them have
    ///   crossed any lines in screen space.
    /// </summary>
    // Todo(iharwell): Implement ISerializable
    [Serializable]
    public class LineCrossingProcessor : IProcessor, ISerializable
    {
        private readonly Detector _detector;

        private LineCrossingProcessor()
        {

        }

        /// <summary>
        ///   Creates a <see cref="LineCrossingProcessor" /> from the provided values.
        /// </summary>
        /// <param name="lineFile">
        ///   The path of a line file describing the lines of interest.
        /// </param>
        /// <param name="samplingFactor">
        ///   The sampling rate factor used by the pipeline. A value of 1 means that all frames are
        ///   used, 2 means that every other frame is used, etc.
        /// </param>
        /// <param name="resFactor">
        ///   The resolution scaling factor used by the pipeline. A value of 1 means that no scaling
        ///   is performed, a value of 0.5 scales the width and height of the frame to half their
        ///   initial size, etc.
        /// </param>
        /// <param name="categories">
        ///   The set of categories of interest, or an empty set to use all categories.
        /// </param>
        /// <param name="bbColor">
        ///   The color of the bounding box used to indicate a positive ID in output images.
        /// </param>
        /// <param name="display">
        ///   <see langword="true" /> to display the output of this stage; <see langword="false" />
        ///   otherwise. Note that in this case, this will output the equivalent to the legacy
        ///   BGSDISPLAY option.
        /// </param>
        public LineCrossingProcessor(string lineFile, int samplingFactor, double resFactor, Color bbColor, bool display = false)
        {
            BoundingBoxColor = bbColor;
            LineFile = lineFile;
            _detector = new Detector(samplingFactor, resFactor, lineFile, display);
            var lines = _detector._multiLaneDetector.GetAllLines();

            DisplayOutput = display;
        }

        public LineCrossingProcessor(SerializationInfo info, StreamingContext context)
        {
            BoundingBoxColor = (Color)info.GetValue(nameof(BoundingBoxColor), typeof(Color));
            LineFile = info.GetString(nameof(LineFile));
            ResolutionFactor = info.GetDouble(nameof(ResolutionFactor));
            SamplingFactor = info.GetInt32(nameof(SamplingFactor));

            _detector = new Detector(SamplingFactor, ResolutionFactor, LineFile, false);
            var lines = _detector._multiLaneDetector.GetAllLines();
            DisplayOutput = info.GetBoolean(nameof(DisplayOutput));
        }

        /// <inheritdoc/>
        public Color BoundingBoxColor { get; set; }

        /// <inheritdoc/>
        public HashSet<string> IncludeCategories { get; set; }
        /// <inheritdoc/>
        [DataMember]
        public HashSet<string> ExcludeCategories { get; set; }

        /// <inheritdoc/>
        public bool DisplayOutput
        {
            get => _detector.DISPLAY_BGS;
            set => _detector.DISPLAY_BGS = value;
        }

        public string LineFile { get; protected set; }

        public int SamplingFactor { get; protected set; }

        public double ResolutionFactor { get; protected set; }

        /// <inheritdoc/>
        public IDictionary<string, LineSegment> LineSegments
        {
            get
            {
                var dict = new Dictionary<string, LineSegment>();

                var segments = this._detector._multiLaneDetector.GetAllLines();
                for (int i = 0; i < segments.Count; i++)
                {
                    dict.Add(segments[i].key, segments[i].segments);
                }
                return dict;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(BoundingBoxColor), BoundingBoxColor);
            info.AddValue(nameof(IncludeCategories), IncludeCategories);
            info.AddValue(nameof(DisplayOutput), DisplayOutput);
            info.AddValue(nameof(LineFile), LineFile);
            info.AddValue(nameof(SamplingFactor), SamplingFactor);
            info.AddValue(nameof(ResolutionFactor), ResolutionFactor);
            info.AddValue(nameof(LineSegments), LineSegments);
        }

        /// <inheritdoc/>
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

        public void RotateLines(int rotateCount, Size frameSize)
        {
            this._detector.RotateLines(rotateCount, frameSize);
        }
    }
}
