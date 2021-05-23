using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.Items
{
    /// <summary>
    /// The default partial implementation of the <see cref="IPathPredictor"/> interface.
    /// </summary>
    public abstract class PathPredictor : IPathPredictor
    {
        public abstract bool CanPredict( IItemPath path, int frameIndex );

        public abstract Rectangle Predict( IItemPath path, int frameIndex );

        public virtual bool TryPredict( IItemPath path, int frameIndex, out Rectangle? prediction )
        {
            if (CanPredict(path,frameIndex))
            {
                prediction = Predict( path, frameIndex );
                return true;
            }
            prediction = null;
            return false;
        }
    }
}
