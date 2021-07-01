// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    public enum AVIODataMarkerType
    {
        /// <summary>
        ///   Header data; this needs to be present for the stream to be decodeable.
        /// </summary>
        AVIO_DATA_MARKER_HEADER,
        /// <summary>
        ///   A point in the output bytestream where a decoder can start decoding (i.e. a
        ///   keyframe). A demuxer/decoder given the data flagged with AVIO_DATA_MARKER_HEADER,
        ///   followed by any AVIO_DATA_MARKER_SYNC_POINT, should give decodeable results.
        /// </summary>
        AVIO_DATA_MARKER_SYNC_POINT,

        /// <summary>
        ///   A point in the output bytestream where a demuxer can start parsing (for non self
        ///   synchronizing bytestream formats). That is, any non-keyframe packet start point.
        /// </summary>
        AVIO_DATA_MARKER_BOUNDARY_POINT,
        /// <summary>
        ///   This is any, unlabelled data. It can either be a muxer not marking any positions at
        ///   all, it can be an actual boundary/sync point that the muxer chooses not to mark, or a
        ///   later part of a packet/fragment that is cut into multiple write callbacks due to
        ///   limited IO buffer size.
        /// </summary>
        AVIO_DATA_MARKER_UNKNOWN,

        /// <summary>
        ///   Trailer data, which doesn't contain actual content, but only for finalizing the output file.
        /// </summary>

        AVIO_DATA_MARKER_TRAILER,
        /// <summary>
        ///   A point in the output bytestream where the underlying AVIOContext might flush the
        ///   buffer depending on latency or buffering requirements. Typically means the end of a packet.
        /// </summary>
        AVIO_DATA_MARKER_FLUSH_POINT,
    }
    public unsafe struct AVIOContext
    {
        public AVClass* av_class;
        public byte* buffer;
        public int buffer_size;
        public byte* buf_ptr;
        public byte* buf_end;
        public void* opaque;
        public delegate* unmanaged[Cdecl]<void*, byte*, int, int> read_packet;
        public delegate* unmanaged[Cdecl]<void*, byte*, int, int> write_packet;

        public delegate* unmanaged[Cdecl]<void*, byte*, int, long> seek;
        public long pos;
        public int eof_reached;
        public int write_flag;
        public int max_packet_size;
        public ulong checksum;
        public byte* checksum_ptr;

        public delegate* unmanaged[Cdecl]<void*, int, int> read_pause;

        public int seekable;
        public long maxsize;
        public int direct;
        public long bytes_read;
        public int seek_count;
        public int writeout_count;
        public int orig_buffer_size;
        public int short_seek_threshold;
        public byte* protocol_whitelist;
        public byte* protocol_blacklist;

        public delegate* unmanaged[Cdecl]<void*, byte*, int, AVIODataMarkerType, long, int> write_data_type;

        public int ignore_boundary_point;
        public AVIODataMarkerType current_type;
        public long last_time;
        public delegate* unmanaged[Cdecl]<void*, int> short_seek_get;

        public long written;
        public byte* buf_ptr_max;
        public int min_packet_size;
    }
}
