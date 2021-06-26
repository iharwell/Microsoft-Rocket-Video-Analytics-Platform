namespace LibAvSharp.Native
{
    unsafe public struct AVFrameSideData
    {
        public AVFrameSideDataType type;
        public byte *data;
        public ulong   size;
        public void *metadata;
        public AVBufferRef *buf;
    }

    public enum AVFrameSideDataType : int
    {
        /*
         * The data is the AVPanScan struct defined in libavcodec.
         */
        AV_FRAME_DATA_PANSCAN,
        /*
         * ATSC A53 Part 4 Closed Captions.
         * A53 CC bitstream is stored as uint8_t in AVFrameSideData.data.
         * The number of bytes of CC data is AVFrameSideData.size.
         */
        AV_FRAME_DATA_A53_CC,
        /*
         * Stereoscopic 3d metadata.
         * The data is the AVStereo3D struct defined in libavutil/stereo3d.h.
         */
        AV_FRAME_DATA_STEREO3D,
        /*
         * The data is the AVMatrixEncoding enum defined in libavutil/channel_layout.h.
         */
        AV_FRAME_DATA_MATRIXENCODING,
        /*
         * Metadata relevant to a downmix procedure.
         * The data is the AVDownmixInfo struct defined in libavutil/downmix_info.h.
         */
        AV_FRAME_DATA_DOWNMIX_INFO,
        /*
         * ReplayGain information in the form of the AVReplayGain struct.
         */
        AV_FRAME_DATA_REPLAYGAIN,
        /*
         * This side data contains a 3x3 transformation matrix describing an affine
         * transformation that needs to be applied to the frame for correct
         * presentation.
         *
         * See libavutil/display.h for a detailed description of the data.
         */
        AV_FRAME_DATA_DISPLAYMATRIX,
        /*
         * Active Format Description data consisting of a single byte as specified
         * in ETSI TS 101 154 using AVActiveFormatDescription enum.
         */
        AV_FRAME_DATA_AFD,
        /*
         * Motion vectors exported by some codecs (on demand through the export_mvs
         * flag set in the libavcodec AVCodecContext flags2 option).
         * The data is the AVMotionVector struct defined in
         * libavutil/motion_vector.h.
         */
        AV_FRAME_DATA_MOTION_VECTORS,
        /*
         * Recommmends skipping the specified number of samples. This is exported
         * only if the "skip_manual" AVOption is set in libavcodec.
         * This has the same format as AV_PKT_DATA_SKIP_SAMPLES.
         * @code
         * u32le number of samples to skip from start of this packet
         * u32le number of samples to skip from end of this packet
         * u8    reason for start skip
         * u8    reason for end   skip (0=padding silence, 1=convergence)
         * @endcode
         */
        AV_FRAME_DATA_SKIP_SAMPLES,
        /*
         * This side data must be associated with an audio frame and corresponds to
         * enum AVAudioServiceType defined in avcodec.h.
         */
        AV_FRAME_DATA_AUDIO_SERVICE_TYPE,
        /*
         * Mastering display metadata associated with a video frame. The payload is
         * an AVMasteringDisplayMetadata type and contains information about the
         * mastering display color volume.
         */
        AV_FRAME_DATA_MASTERING_DISPLAY_METADATA,
        /*
         * The GOP timecode in 25 bit timecode format. Data format is 64-bit integer.
         * This is set on the first frame of a GOP that has a temporal reference of 0.
         */
        AV_FRAME_DATA_GOP_TIMECODE,

        /*
         * The data represents the AVSphericalMapping structure defined in
         * libavutil/spherical.h.
         */
        AV_FRAME_DATA_SPHERICAL,

        /*
         * Content light level (based on CTA-861.3). This payload contains data in
         * the form of the AVContentLightMetadata struct.
         */
        AV_FRAME_DATA_CONTENT_LIGHT_LEVEL,

        /*
         * The data contains an ICC profile as an opaque octet buffer following the
         * format described by ISO 15076-1 with an optional name defined in the
         * metadata key entry "name".
         */
        AV_FRAME_DATA_ICC_PROFILE,

        /*
         * Timecode which conforms to SMPTE ST 12-1. The data is an array of 4 uint32_t
         * where the first uint32_t describes how many (1-3) of the other timecodes are used.
         * The timecode format is described in the documentation of av_timecode_get_smpte_from_framenum()
         * function in libavutil/timecode.h.
         */
        AV_FRAME_DATA_S12M_TIMECODE,

        /*
         * HDR dynamic metadata associated with a video frame. The payload is
         * an AVDynamicHDRPlus type and contains information for color
         * volume transform - application 4 of SMPTE 2094-40:2016 standard.
         */
        AV_FRAME_DATA_DYNAMIC_HDR_PLUS,

        /*
         * Regions Of Interest, the data is an array of AVRegionOfInterest type, the number of
         * array element is implied by AVFrameSideData.size / AVRegionOfInterest.self_size.
         */
        AV_FRAME_DATA_REGIONS_OF_INTEREST,

        /*
         * Encoding parameters for a video frame, as described by AVVideoEncParams.
         */
        AV_FRAME_DATA_VIDEO_ENC_PARAMS,

        /*
         * User data unregistered metadata associated with a video frame.
         * This is the H.26[45] UDU SEI message, and shouldn't be used for any other purpose
         * The data is stored as uint8_t in AVFrameSideData.data which is 16 bytes of
         * uuid_iso_iec_11578 followed by AVFrameSideData.size - 16 bytes of user_data_payload_byte.
         */
        AV_FRAME_DATA_SEI_UNREGISTERED,

        /*
         * Film grain parameters for a frame, described by AVFilmGrainParams.
         * Must be present for every frame which should have film grain applied.
         */
        AV_FRAME_DATA_FILM_GRAIN_PARAMS,

        /*
         * Bounding boxes for object detection and classification,
         * as described by AVDetectionBBoxHeader.
         */
        AV_FRAME_DATA_DETECTION_BBOXES,
    }
}