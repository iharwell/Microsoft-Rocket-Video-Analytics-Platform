using System.Collections.Generic;
using System.Drawing;
using DNNDetector.Model;
using OpenCvSharp;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    /// <summary>
    ///   Represents a processor in the pipeline. These processors identify items in a provided frame.
    /// </summary>
    public interface IProcessor
    {
        /// <summary>
        ///   The brush used to draw the bounding box around any identified item when saving.
        /// </summary>
        Color BoundingBoxColor { get; set; }

        /// <summary>
        ///   The line segments used to detect objects, if available.
        /// </summary>
        IDictionary<string, LineSegment> LineSegments { get; set; }

        /// <summary>
        ///   A set of the categories to look for.
        /// </summary>
        ISet<string> Categories { get; set; }

        /// <summary>
        ///   Processes the frame, adds any results to the provided list of items, and returns a
        ///   boolean indicating whether or not this processor found any items.
        /// </summary>
        /// <param name="frame">
        ///   The frame to process.
        /// </param>
        /// <param name="items">
        ///   A running list of items found in each stage, with each stage adding entries to
        ///   existing items where appropriate.
        /// </param>
        /// <param name="previousStage">
        ///   A reference to the previous stage in the pipeline. Used for sifting through items.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if an item was found; otherwise <see langword="false" />.
        /// </returns>
        bool Run( IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage );

        bool DisplayOutput { get; set; }
    }
}