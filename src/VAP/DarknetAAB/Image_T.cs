// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarknetAAB
{
    public unsafe struct Image_T
    {
        public int h;
        public int w;
        public int c;
        private float* data;
    }
}
