// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LibAvSharp.Native
{
    public unsafe struct AVFilterContext
    {
        public AVClass* av_class;
        public AVFilter* filter;
        public byte* name;
        public AVFilterPad* input_pads;
        public AVFilterLink** inputs;
        public uint nb_inputs;
        public AVFilterPad* output_pads;
        public AVFilterLink** outputs;
        public uint nb_outputs;
        public void* priv;
        public AVFilterGraph* graph;
        public int thread_type;
        public AVFilterInternal* _internal;
        public AVFilterCommand* command_queue;
        public byte* enable_str;
        public void* enable;
        public double* var_values;
        public int is_disabled;
        public AVBufferRef* hw_device_ctx;
        public int nb_threads;
        public uint ready;

    }
}
