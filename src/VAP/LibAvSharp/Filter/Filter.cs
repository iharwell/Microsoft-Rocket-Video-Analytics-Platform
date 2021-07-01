// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibAvSharp.Native;

namespace LibAvSharp.Filter
{
    public unsafe class Filter
    {
        internal AVFilter* _filter;

        static Filter()
        {
            //AVFilterC.avfilter_register_all();
        }

        public Filter(string name)
        {
            _filter = AVFilterC.avfilter_get_by_name(name);
        }
    }
}
