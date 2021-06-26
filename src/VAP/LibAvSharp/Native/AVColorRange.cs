namespace LibAvSharp.Native
{
    public enum AVColorRange : int
    {
        AVCOL_RANGE_UNSPECIFIED = 0,

        /*
         * Narrow or limited range content.
         *
         * - For luma planes:
         *
         *       (219 * E + 16) * 2^(n-8)
         *
         *   F.ex. the range of 16-235 for 8 bits
         *
         * - For chroma planes:
         *
         *       (224 * E + 128) * 2^(n-8)
         *
         *   F.ex. the range of 16-240 for 8 bits
         */
        AVCOL_RANGE_MPEG        = 1,

        /*
         * Full range content.
         *
         * - For RGB and luma planes:
         *
         *       (2^n - 1) * E
         *
         *   F.ex. the range of 0-255 for 8 bits
         *
         * - For chroma planes:
         *
         *       (2^n - 1) * E + 2^(n - 1)
         *
         *   F.ex. the range of 1-255 for 8 bits
         */
        AVCOL_RANGE_JPEG        = 2,
        AVCOL_RANGE_NB               //< Not part of ABI
    }
}