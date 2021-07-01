// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using System.Runtime.InteropServices;

namespace LibAvSharp.Native
{
    public enum AVOptionType : int
    {
        AV_OPT_TYPE_FLAGS,
        AV_OPT_TYPE_INT,
        AV_OPT_TYPE_INT64,
        AV_OPT_TYPE_DOUBLE,
        AV_OPT_TYPE_FLOAT,
        AV_OPT_TYPE_STRING,
        AV_OPT_TYPE_RATIONAL,
        AV_OPT_TYPE_BINARY,  //< offset must point to a pointer immediately followed by an int for the length
        AV_OPT_TYPE_DICT,
        AV_OPT_TYPE_UINT64,
        AV_OPT_TYPE_CONST,
        AV_OPT_TYPE_IMAGE_SIZE, //< offset must point to two consecutive integers
        AV_OPT_TYPE_PIXEL_FMT,
        AV_OPT_TYPE_SAMPLE_FMT,
        AV_OPT_TYPE_VIDEO_RATE, //< offset must point to AVRational
        AV_OPT_TYPE_DURATION,
        AV_OPT_TYPE_COLOR,
        AV_OPT_TYPE_CHANNEL_LAYOUT,
        AV_OPT_TYPE_BOOL,
    }

    [Flags]
    public enum AVOptionFlags : int
    {
        AV_OPT_FLAG_ENCODING_PARAM = 1 << 0,
        AV_OPT_FLAG_DECODING_PARAM = 1 << 1,
        AV_OPT_FLAG_AUDIO_PARAM = 1 << 3,
        AV_OPT_FLAG_VIDEO_PARAM = 1 << 4,
        AV_OPT_FLAG_SUBTITLE_PARAM = 1 << 5,
        AV_OPT_FLAG_EXPORT = 1 << 6,
        AV_OPT_FLAG_READONLY = 1 << 7,
        AV_OPT_FLAG_BSF_PARAM = 1 << 8,
        AV_OPT_FLAG_RUNTIME_PARAM = 1 << 15,
        AV_OPT_FLAG_FILTERING_PARAM = 1 << 16,
        AV_OPT_FLAG_DEPRECATED = 1 << 17,
        AV_OPT_FLAG_CHILD_CONSTS = 1 << 18,
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct AVOptionValue
    {
        [FieldOffset(0)]
        private long i64;
        [FieldOffset(0)]
        private double dbl;
        [FieldOffset(0)]
        private char* str;
        [FieldOffset(0)]
        private AVRational rational;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AVOption
    {
        public char* name;
        public char* help;
        public int offset;
        public AVOptionType optiontype;
        public AVOptionValue default_val;
        public double min;
        public double max;
        public char* unit;
    }
}
