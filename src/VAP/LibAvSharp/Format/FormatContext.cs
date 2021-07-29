﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LibAvSharp.Codec;
using LibAvSharp.Native;
using LibAvSharp.Util;

namespace LibAvSharp.Format
{
    public unsafe class FormatContext : IDisposable
    {
        internal AVFormatContext* native_context;
        private bool disposedValue;
        private string fileName;

        public FormatContext()
        {
        }

        public AVStream StreamItem(int index)
        {
            return new AVStream(native_context->streams[index]);
        }

        public void DumpFormat(int index, int is_output)
        {
            AVException.ProcessException(AVFormatC.av_dump_format(native_context, index, fileName, is_output));
        }
        public void OpenInput(string file)
        {
            AVException.ProcessException(AVFormatC.avformat_open_input(ref native_context, file, null, null));
            fileName = file;
        }

        public int ReadFrame(ref Packet p)
        {
            p.PacketReffed = true;
            int ret = AVFormatC.av_read_frame(native_context, p._packet);
            if(ret<0)
            {
                p.PacketReffed = false;
            }
            return ret;
        }

        public void FindStreamInfo()
        {
            AVException.ProcessException(AVFormatC.avformat_find_stream_info(native_context, null));
        }

        public CodecContext OpenCodecContext(ref int streamIndex, AVMediaType mediaType, AVPixelFormat format = AVPixelFormat.AV_PIX_FMT_NONE)
        {
            Native.AVStream* st;
            AVCodec* dec;
            AVDictionary* dict = null;
            streamIndex = AVFormatC.av_find_best_stream(native_context, mediaType, -1, -1, IntPtr.Zero, 0);
            if (streamIndex < 0)
            {
                var name = Marshal.PtrToStringAnsi(new IntPtr(AVUtilsC.av_get_media_type_string(mediaType)));
                throw new Exception("Could not find " + name + " stream in input file " + fileName);
            }
            else
            {
                st = native_context->streams[streamIndex];
                dec = findCodecHWAccel(st->codecpar->codec_id);

                if (dec == null)
                {
                    throw new NullReferenceException();
                }

                AVCodecContext* dec_ctx = AVCodecC.avcodec_alloc_context3(dec);
                CodecContext context = new CodecContext(dec_ctx);
                if (dec_ctx == null)
                {
                    throw new NullReferenceException();
                }
                AVException.ProcessException(AVCodecC.avcodec_parameters_to_context(context._native_context, st->codecpar));
                context.PixelFormat = format;

                AVException.ProcessException(AVCodecC.avcodec_open2(context._native_context, dec, &dict));
                return context;
            }
        }

        public void Close()
        {
            AVException.ProcessException(AVFormatC.avformat_close_input(ref native_context));
        }

        private static AVCodec* findCodecHWAccel(AVCodecID codec_id)
        {
            string name = AVCodecC.avcodec_get_name(codec_id);

            StringBuilder sb = new StringBuilder(name);
            sb.Append("_cuvid");
            string hwName = sb.ToString();
            var codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_dxva2");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_qsv");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_vdpau");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_videotoolbox");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_uvd");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            return AVCodecC.avcodec_find_decoder(codec_id);
        }
        private static AVCodec* findCodecHWAccel(AVCodecID codec_id, double scale)
        {
            string name = AVCodecC.avcodec_get_name(codec_id);

            StringBuilder sb = new StringBuilder(name);
            sb.Append("_cuvid");
            string hwName = sb.ToString();
            var codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_dxva2");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_qsv");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_vdpau");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_videotoolbox");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            sb.Clear();
            sb.Append(name);
            sb.Append("_uvd");
            hwName = sb.ToString();
            codec = AVCodecC.avcodec_find_decoder_by_name(hwName);
            if (codec != null)
            {
                return codec;
            }

            return AVCodecC.avcodec_find_decoder(codec_id);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                if (native_context != null)
                {
                    AVException.ProcessException(AVFormatC.avformat_close_input(ref native_context));
                }

                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~FormatContext()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}