namespace LibAvSharp.Native
{
    unsafe public struct AVFrame
    {
        // Sure would be nice if this worked:
        // public fixed byte* data[8];
        public byte* data(int index)
        {
            switch (index)
            {
                case 0: return data0;
                case 1: return data1;
                case 2: return data2;
                case 3: return data3;
                case 4: return data4;
                case 5: return data5;
                case 6: return data6;
                case 7: return data7;
                default:
                    throw new System.IndexOutOfRangeException();
            }
        }

        public byte* data0;
        public byte* data1;
        public byte* data2;
        public byte* data3;
        public byte* data4;
        public byte* data5;
        public byte* data6;
        public byte* data7;

        public int linesize(int index)
        {
            switch (index)
            {
                case 0: return linesize0;
                case 1: return linesize1;
                case 2: return linesize2;
                case 3: return linesize3;
                case 4: return linesize4;
                case 5: return linesize5;
                case 6: return linesize6;
                case 7: return linesize7;
                default:
                    throw new System.IndexOutOfRangeException();
            }
        }
        public int linesize0;
        public int linesize1;
        public int linesize2;
        public int linesize3;
        public int linesize4;
        public int linesize5;
        public int linesize6;
        public int linesize7;
        public byte** extended_data;
        public int width;
        public int height;
        public int nb_samples;
        public int format;
        public int key_frame;
        public AVPictureType pict_type;
        public AVRational sample_aspect_ratio;
        public long pts;
        public long pkt_dts;
        public int coded_picture_number;
        public int display_picture_number;
        public int quality;
        public void* opaque;
        public int repeat_pict;
        public int interlaced_frame;
        public int top_field_first;
        public int palette_has_changed;
        public long reordered_opaque;
        public int sample_rate;
        public ulong channel_layout;


        // Sure would be nice if this worked.
        // public fixed AVBufferRef* buf[8];
        public AVBufferRef* buf(int index)
        {
            if (index >= 8 || index < 0)
            {
                throw new System.IndexOutOfRangeException();
            }
            switch (index)
            {
                case 0:
                    return buf0;
                case 1:
                    return buf1;
                case 2:
                    return buf2;
                case 3:
                    return buf3;
                case 4:
                    return buf4;
                case 5:
                    return buf5;
                case 6:
                    return buf6;
                case 7:
                    return buf7;
                default:
                    throw new System.IndexOutOfRangeException();
            }

        }

        public AVBufferRef* buf0;
        public AVBufferRef* buf1;
        public AVBufferRef* buf2;
        public AVBufferRef* buf3;
        public AVBufferRef* buf4;
        public AVBufferRef* buf5;
        public AVBufferRef* buf6;
        public AVBufferRef* buf7;
        public AVBufferRef** extended_buf;
        public int nb_extended_buf;
        public AVFrameSideData** side_data;
        public int nb_side_data;
        public int flags;
        public AVColorRange color_range;
        public AVColorPrimaries color_primaries;
        public AVColorTransferCharacteristic color_trc;
        public AVColorSpace colorspace;
        public AVChromaLocation chroma_location;
        public long best_effort_timestamp;
        public long pkt_pos;
        public long pkt_duration;
        public void* metadata;
        public int decode_error_flags;
        public int channels;
        public int pkt_size;
        public AVBufferRef* hw_frames_ctx;
        public AVBufferRef* opaque_ref;
        public ulong crop_top;
        public ulong crop_bottom;
        public ulong crop_left;
        public ulong crop_right;
        public AVBufferRef* private_ref;
    }
}
