namespace LibAvSharp.Native
{
    public enum AVSubtitleType : int
    {
        SUBTITLE_NONE,

        SUBTITLE_BITMAP,                //< A bitmap, pict will be set

        /*
         * Plain text, the text field must be set by the decoder and is
         * authoritative. ass and pict fields may contain approximations.
         */
        SUBTITLE_TEXT,

        /*
         * Formatted text, the ass field must be set by the decoder and is
         * authoritative. pict and text fields may contain approximations.
         */
        SUBTITLE_ASS,
    }

    unsafe public struct AVSubtitleRect
    {
        public int x;         //< top left corner  of pict, undefined when pict is not set
        public int y;         //< top left corner  of pict, undefined when pict is not set
        public int w;         //< width            of pict, undefined when pict is not set
        public int h;         //< height           of pict, undefined when pict is not set
        public int nb_colors; //< number of colors in pict, undefined when pict is not set

        public byte* data0;
        public byte* data1;
        public byte* data2;
        public byte* data3;

        public int linesize0;
        public int linesize1;
        public int linesize2;
        public int linesize3;

        public AVSubtitleType type;
        public char* text;
        public char* ass;
        public int flags;
    }
}