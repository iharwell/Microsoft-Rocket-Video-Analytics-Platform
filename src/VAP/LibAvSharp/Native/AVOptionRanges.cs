// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    public unsafe struct AVOptionRange
    {
        public char* str;
        public double value_min;
        public double value_max;
        public double component_min;
        public double component_max;
        public int is_range;
    }

    public unsafe struct AVOptionRanges
    {
        /// <summary>
        ///   Array of option ranges.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Most of option types use just one component.
        ///     Following describes multi-component option types:
        ///   </para>
        /// 
        ///   <list><listheader>AV_OPT_TYPE_IMAGE_SIZE:</listheader>
        ///   <item><term>component index 0:</term><description>range of pixel count (width * height).</description></item>
        ///   <item><term>component index 1:</term> <description>range of width.</description></item>
        ///   <item><term>component index 2:</term> <description>range of height.</description></item></list>
        /// 
        /// <para>
        /// Note: To obtain multi-component version of this structure, user must
        ///       provide AV_OPT_MULTI_COMPONENT_RANGE to av_opt_query_ranges or
        ///       av_opt_query_ranges_default function.</para>
        /// 
        /// <para>Multi-component range can be read as in following example:</para>
        /// 
        /// <code>
        /// int range_index, component_index;
        /// AVOptionRanges *ranges;
        /// AVOptionRange *range[3]; //may require more than 3 in the future.
        /// av_opt_query_ranges(&ranges, obj, key, AV_OPT_MULTI_COMPONENT_RANGE);
        /// for (range_index = 0; range_index &lt ranges->nb_ranges; range_index++) {
        ///     for (component_index = 0; component_index &lt ranges->nb_components; component_index++)
        ///         range[component_index] = ranges->range[ranges->nb_ranges * component_index + range_index];
        ///     //do something with range here.
        /// }
        /// av_opt_freep_ranges(&ranges);
        /// </code>
        /// </remarks>
        public AVOptionRange** range;

        /// <summary>
        ///   Number of ranges per component.
        /// </summary>
        public int nb_ranges;

        /// <summary>
        ///   Number of components.
        /// </summary>
        public int nb_components;
    }
}
