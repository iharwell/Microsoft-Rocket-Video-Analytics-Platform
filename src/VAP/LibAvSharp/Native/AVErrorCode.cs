// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    public enum AVErrorCode : int
    {
        AV_EAGAIN = -11,
        AV_EOF = -('E' | ('O' << 8) | ('F' << 16) | (' ' << 24)),
        AV_EXIT = -('E' | ('X' << 8) | ('I' << 16) | ('T' << 24)),

    }
}
