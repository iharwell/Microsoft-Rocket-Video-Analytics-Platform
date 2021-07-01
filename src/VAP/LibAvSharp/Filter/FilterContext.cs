// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using LibAvSharp.Native;
using LibAvSharp.Util;

namespace LibAvSharp.Filter
{
    public unsafe class FilterContext
    {
        private static readonly string FormatSettingName = "pix_fmts";

        public IntPtr InternalContext => (IntPtr)_context;

        [Flags]
        private enum BufferSourceFlags : int
        {
            AV_BUFFERSRC_FLAG_NO_CHECK_FORMAT = 1,
            AV_BUFFERSRC_FLAG_PUSH = 4,
            AV_BUFFERSRC_FLAG_KEEP_REF = 8,

        }
        internal AVFilterContext* _context;

        public FilterContext()
        {

        }

        public FilterContext(AVFilterContext* ptr)
        {
            _context = ptr;
        }

        public void PushFrame(Frame frame)
        {
            AVFilterC.av_buffersrc_add_frame_flags(_context, frame._frame, (int)BufferSourceFlags.AV_BUFFERSRC_FLAG_NO_CHECK_FORMAT);
        }
        public int PullFrame(ref Frame frame)
        {
            return AVFilterC.av_buffersink_get_frame(_context, frame._frame);
        }

        public AVPixelFormat[] FormatSetting
        {
            get
            {
                return null;
            }
            set
            {
                int[] input = new int[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    input[i] = (int)value[i];
                }
                AVOptionsC.SetIntList(_context, FormatSettingName, input, (int)AVOptionSearchFlags.AV_OPT_SEARCH_CHILDREN);
            }
        }
    }
}
