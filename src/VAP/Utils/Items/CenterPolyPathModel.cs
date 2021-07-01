// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.Items
{

    public class CenterPolyPathModel
    {
        public double[] CenterXCoefs { get; set; }
        public double[] CenterYCoefs { get; set; }
        public double[] WidthCoefs { get; set; }
        public double[] HeightCoefs { get; set; }

        public RectangleF Predict(int frameNumber)
        {
            RectangleF rect = new RectangleF();

            PointF center = new PointF
            {
                X = (float)MathNet.Numerics.Polynomial.Evaluate((double)frameNumber, CenterXCoefs),
                Y = (float)MathNet.Numerics.Polynomial.Evaluate((double)frameNumber, CenterYCoefs)
            };
            rect.Width = (float)MathNet.Numerics.Polynomial.Evaluate((double)frameNumber, WidthCoefs);
            rect.Height = (float)MathNet.Numerics.Polynomial.Evaluate((double)frameNumber, HeightCoefs);

            rect.X = center.X - rect.Width / 2;
            rect.Y = center.Y - rect.Height / 2;
            return rect;
        }
    }
}
