// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Utils.Items;

namespace Utils
{
    public interface IDNNAnalyzer
    {
        IEnumerable<IItemID> Analyze(Mat frameData, ISet<string> category, object sourceObject);
    }
}
