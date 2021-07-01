// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    [Flags]
    public enum AVOptionSearchFlags : int
    {
        AV_OPT_SEARCH_CHILDREN         = 1 <<  0,
        AV_OPT_SEARCH_FAKE_OBJ         = 1 <<  1,
        AV_OPT_ALLOW_NULL              = 1 <<  2,
        AV_OPT_MULTI_COMPONENT_RANGE   = 1 << 12,
        AV_OPT_SERIALIZE_SKIP_DEFAULTS = 1 <<  1,
    }
}
