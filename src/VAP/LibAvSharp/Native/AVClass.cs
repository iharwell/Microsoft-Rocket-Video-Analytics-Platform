using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace LibAvSharp.Native
{
    public enum AVClassCategory : int
    {
        AV_CLASS_CATEGORY_NA = 0,
        AV_CLASS_CATEGORY_INPUT,
        AV_CLASS_CATEGORY_OUTPUT,
        AV_CLASS_CATEGORY_MUXER,
        AV_CLASS_CATEGORY_DEMUXER,
        AV_CLASS_CATEGORY_ENCODER,
        AV_CLASS_CATEGORY_DECODER,
        AV_CLASS_CATEGORY_FILTER,
        AV_CLASS_CATEGORY_BITSTREAM_FILTER,
        AV_CLASS_CATEGORY_SWSCALER,
        AV_CLASS_CATEGORY_SWRESAMPLER,
        AV_CLASS_CATEGORY_DEVICE_VIDEO_OUTPUT = 40,
        AV_CLASS_CATEGORY_DEVICE_VIDEO_INPUT,
        AV_CLASS_CATEGORY_DEVICE_AUDIO_OUTPUT,
        AV_CLASS_CATEGORY_DEVICE_AUDIO_INPUT,
        AV_CLASS_CATEGORY_DEVICE_OUTPUT,
        AV_CLASS_CATEGORY_DEVICE_INPUT,
        AV_CLASS_CATEGORY_NB
    }
    unsafe public struct AVClass
    {
        /// <summary>
        ///   The name of the class; usually it is the same name as the context structure type to
        ///   which the AVClass is associated.
        /// </summary>
        public byte* className;

        /// <summary>
        ///   A pointer to a function which returns the name of a context instance ctx associated
        ///   with the class.
        /// </summary>
        public delegate * unmanaged[Cdecl]<void*, string> itemName;

        /// <summary>
        ///   a pointer to the first option specified in the class if any or NULL. see av_set_default_options()
        /// </summary>
        public AVOption* option;

        /// <summary>
        ///   LIBAVUTIL_VERSION with which this structure was created. This is used to allow fields
        ///   to be added without requiring major version bumps everywhere.
        /// </summary>
        public int version;

        /// <summary>
        ///   Offset in the structure where log_level_offset is stored. 0 means there is no such variable
        /// </summary>
        public int log_level_offset_offset;


        /// <summary>
        ///   Offset in the structure where a pointer to the parent context for logging is stored.
        ///   For example a decoder could pass its AVCodecContext to eval as such a parent context,
        ///   which an av_log() implementation could then leverage to display the parent context.
        ///   The offset can be NULL.
        /// </summary>
        public int parent_log_context_offset;

        /// <summary>
        ///   Return next AVOptions-enabled child or NULL
        /// </summary>
        public delegate* unmanaged[Cdecl]<void*, void*, void*> child_next;

        /// <summary>
        ///   Category used for visualization( like color ) This is only set if the category
        ///   is equal for all objects using this class. available since version(51 &lt&lt 16 | 56
        ///   &lt&lt 8 | 100)
        /// </summary>
        public AVClassCategory category;

        /// <summary>
        ///   Callback to return the category. available since version (51 &lt&lt 16 | 59 
        ///   &lt&lt 8 | 100)
        /// </summary>
        public delegate* unmanaged[Cdecl]<void*,AVClassCategory> get_category;


        /// <summary>
        ///   Callback to return the supported/allowed ranges. available since version (52.12)
        /// </summary>
        // int (* query_ranges) (struct AVOptionRanges **, void *obj, const char *key, int flags);
        public delegate* unmanaged[Cdecl]<AVOptionRanges**, void*, string, int, int> query_ranges;

        public delegate* unmanaged[Cdecl]<void**, AVClass> child_class_iterate;
    }
}
