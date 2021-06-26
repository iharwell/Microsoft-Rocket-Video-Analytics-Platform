namespace LibAvSharp.Native
{
    public unsafe struct AVBufferRef
    {
        public void *buffer;

        /*
         * The data buffer. It is considered writable if and only if
         * this is the only reference to the buffer, in which case
         * av_buffer_is_writable() returns 1.
         */
        public byte *data;
        /*
         * Size of data in bytes.
         */
        public long   size;
    }
}