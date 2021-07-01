// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class Formulas
    {
        public static (int a, int b, int c) LineEquation(Point p1, Point p2)
        {
            int a = p2.Y - p1.Y;
            int b = p1.X - p2.X;
            int c = -(a * p1.X + b * p2.Y);

            return (a, b, c);
        }
    }
}
