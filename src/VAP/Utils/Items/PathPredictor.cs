using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    ///   The default abstract implementation of the <see cref="IPathPredictor" /> interface.
    /// </summary>
    public abstract class PathPredictor : IPathPredictor
    {
        /// <inheritdoc />
        public abstract bool CanPredict(IItemPath path, int frameIndex);

        /// <inheritdoc />
        public abstract Rectangle Predict(IItemPath path, int frameIndex);

        /// <inheritdoc />
        public virtual bool TryPredict(IItemPath path, int frameIndex, out Rectangle? prediction)
        {
            if (CanPredict(path, frameIndex))
            {
                prediction = Predict(path, frameIndex);
                return true;
            }
            prediction = null;
            return false;
        }
    }
}
