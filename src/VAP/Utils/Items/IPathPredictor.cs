// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    ///   Exposes path prediction functionality.
    /// </summary>
    public interface IPathPredictor
    {
        /// <summary>
        ///   Determines whether or not this <see cref="IPathPredictor" /> is capable of predicting
        ///   the location of an item in the given frame.
        /// </summary>
        /// <param name="path">
        ///   The known path of the item.
        /// </param>
        /// <param name="frameIndex">
        ///   The index of the frame to predict.
        /// </param>
        /// <returns>
        ///   Returns true if the location of the object can be predicted, and false otherwise.
        /// </returns>
        bool CanPredict(IItemPath path, int frameIndex);

        /// <summary>
        ///   Predicts the location of the item at the provided frame index.
        /// </summary>
        /// <param name="path">
        ///   The known path of the item.
        /// </param>
        /// <param name="frameIndex">
        ///   The index of the frame to predict.
        /// </param>
        /// <returns>
        ///   Returns the predicted bounding box of the item at the given frame index.
        /// </returns>
        Rectangle Predict(IItemPath path, int frameIndex);

        /// <summary>
        ///   Attempts to predict the location of the item at the provided frame index.
        /// </summary>
        /// <param name="path">
        ///   The known path of the item.
        /// </param>
        /// <param name="frameIndex">
        ///   The index of the frame to predict.
        /// </param>
        /// <param name="prediction">
        ///   The prediction result if successful, and null otherwise.
        /// </param>
        /// <returns>
        ///   Returns true if the prediction was successful, and false otherwise.
        /// </returns>
        bool TryPredict(IItemPath path, int frameIndex, out Rectangle? prediction);
    }
}
