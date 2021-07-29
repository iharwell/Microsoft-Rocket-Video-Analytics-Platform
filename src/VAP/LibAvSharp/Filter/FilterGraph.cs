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
    public unsafe class FilterGraph
    {
        internal AVFilterGraph* _graph;

        public FilterGraph()
        {
            _graph = AVFilterC.avfilter_graph_alloc();
        }

        public void CreateFilter(ref FilterContext filterContext, Filter filter, string name, string args, IntPtr opaque)
        {
            AVFilterContext* ptr = null;
            AVException.ProcessException(AVFilterC.avfilter_graph_create_filter(&ptr, filter._filter, name, args, (void*)opaque, _graph));

            filterContext._context = ptr;
        }

        public void Parse(string filters_desc, FilterInOut[] inputs, FilterInOut[] outputs)
        {
            AVFilterInOut*[] inputPtrs = new AVFilterInOut*[inputs.Length];
            AVFilterInOut*[] outputPtrs = new AVFilterInOut*[outputs.Length];

            int res;

            for (int i = 0; i < inputPtrs.Length; i++)
            {
                inputPtrs[i] = inputs[i]._filterIO;
            }
            for (int i = 0; i < outputPtrs.Length; i++)
            {
                outputPtrs[i] = outputs[i]._filterIO;
            }
            fixed (AVFilterInOut** inptrs = &inputPtrs[0])
            fixed (AVFilterInOut** outptrs = &outputPtrs[0])
            {
                res = AVFilterC.avfilter_graph_parse_ptr(_graph, filters_desc, inptrs, outptrs, null);
            }
            AVException.ProcessException(res);
            for (int i = 0; i < inputPtrs.Length; i++)
            {
                inputs[i]._filterIO = inputPtrs[i];
            }
            for (int i = 0; i < outputPtrs.Length; i++)
            {
                outputs[i]._filterIO = outputPtrs[i];
            }
        }

        public void Config()
        {
            AVException.ProcessException(AVFilterC.avfilter_graph_config(_graph, null));
        }

    }
}
