// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    /// <summary>
    ///   A heavy darknet Yolo processing stage used to identify items that have been flagged by the previous stage.
    /// </summary>
    public class HeavyDarknetProcessor : IProcessor
    {
        private readonly DarknetDetector.CascadedDNNDarknet _cascadedDNN;

        /// <summary>
        ///   Creates a new <see cref="HeavyDarknetProcessor" />.
        /// </summary>
        /// <param name="categories">
        ///   A set of categories that are of interest to the analysis or an empty set if all
        ///   categories are interesting.
        /// </param>
        /// <param name="resFactor">
        ///   The resolution scaling factor used by the pipeline.
        /// </param>
        /// <param name="boxColor">
        ///   The color of the box that this stage uses to mark identified items.
        /// </param>
        /// <param name="displayOutput">
        ///   <see langword="true"/> to display the output of this stage; <see langword="false"/> otherwise.
        /// </param>
        public HeavyDarknetProcessor(ISet<string> categories,
                                      double resFactor,
                                      Color boxColor,
                                      bool displayOutput)
        {
            _cascadedDNN = new DarknetDetector.CascadedDNNDarknet(resFactor);
            Categories = categories;
            BoundingBoxColor = boxColor;
            DisplayOutput = displayOutput;
        }

        public HeavyDarknetProcessor(IList<(string, LineSegment)> lines,
                                      ISet<string> categories,
                                      double resFactor,
                                      Color boxColor,
                                      bool displayOutput)
            : this(categories, resFactor, boxColor, displayOutput)
        {
            if (lines != null)
            {
                LineSegments = new Dictionary<string, LineSegment>(from entry in lines select new KeyValuePair<string, LineSegment>(entry.Item1, entry.Item2));
            }
            else
            {
                LineSegments = new Dictionary<string, LineSegment>();
            }
        }

        /// <summary>
        ///   Creates a new <see cref="HeavyDarknetProcessor" />.
        /// </summary>
        /// <param name="lines">
        ///   A dictionary of lines used by the pipeline organized by name.
        /// </param>
        /// <param name="categories">
        ///   A set of categories that are of interest to the analysis or an empty set if all
        ///   categories are interesting.
        /// </param>
        /// <param name="resFactor">
        ///   The resolution scaling factor used by the pipeline.
        /// </param>
        /// <param name="boxColor">
        ///   The color of the box that this stage uses to mark identified items.
        /// </param>
        /// <param name="displayOutput">
        ///   <see langword="true" /> to display the output of this stage; <see langword="false" /> otherwise.
        /// </param>
        public HeavyDarknetProcessor(IDictionary<string, LineSegment> lines,
                                      ISet<string> categories,
                                      double resFactor,
                                      Color boxColor,
                                      bool displayOutput)
            : this(categories, resFactor, boxColor, displayOutput)
        {
            LineSegments = lines;
        }

        /// <inheritdoc/>
        public Color BoundingBoxColor { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, LineSegment> LineSegments { get; set; }

        /// <inheritdoc/>
        public ISet<string> Categories { get; set; }

        /// <inheritdoc/>
        public bool DisplayOutput { get; set; }

        /// <inheritdoc/>
        public bool Run(IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage)
        {
            items = _cascadedDNN.Run(frame, items, Categories, previousStage, this);

            if (items == null)
            {
                return false;
            }

            for (int i = items.Count - 1; i >= 0; --i)
            {
                var item = items[i];
                for (int j = item.ItemIDs.Count - 1; j >= 0; --j)
                {
                    var id = item.ItemIDs[j];
                    if (id.SourceObject == this)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
