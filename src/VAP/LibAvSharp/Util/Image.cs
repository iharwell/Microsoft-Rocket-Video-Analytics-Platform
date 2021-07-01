// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LibAvSharp.Native;

namespace LibAvSharp.Util
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AVImageCore
    {
        internal byte* data0;
        internal byte* data1;
        internal byte* data2;
        internal byte* data3;

        internal int size0;
        internal int size1;
        internal int size2;
        internal int size3;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct UCSizeStructure
    {
        internal ulong size0;
        internal ulong size1;
        internal ulong size2;
        internal ulong size3;
    }

    public unsafe class Image
    {
        internal AVImageCore _imageCore;
        internal AVPixelFormat _format;
        internal int _height;
        internal int _width;
        internal int _bufSize;

        /*public Image( int height, int width, AVPixelFormat format, AVImageCore core, int bufSize )
        {
            _height = height;
            _width = width;
            _format = format;
            _imageCore = core;
            _bufSize = bufSize;
        }*/

        public Image(int height, int width, AVPixelFormat format)
        {
            var imc = new AVImageCore();
            byte** srcd = &imc.data0;
            _bufSize = AVImgUtilsC.av_image_alloc(&imc.data0, &imc.size0, width, height, format, 1);
            _imageCore = imc;
            _format = format;
            _height = height;
            _width = width;
        }

        public AVPixelFormat Format
        {
            get => _format;
            set => _format = value;
        }
        public int Height
        {
            get => _height;
            set => _height = value;
        }
        public int Width
        {
            get => _width;
            set => _width = value;
        }

        public static void CopyImage(Image src, Image dst)
        {
            fixed (AVImageCore* srcptr = &src._imageCore)
            fixed (AVImageCore* dstptr = &dst._imageCore)
            {
                byte** srcd = (byte**)((void*)(&srcptr->data0));
                byte** dstd = (byte**)((void*)(&dstptr->data0));

                int* srcs = &(srcptr->size0);
                int* dsts = &(dstptr->size0);
                AVImgUtilsC.av_image_copy(dstd, dsts, srcd, srcs, src.Format, src.Width, src.Height);
            }
        }

        public static void CopyImageUc(Image src, Image dst)
        {
            UCSizeStructure srcSizes = new UCSizeStructure();
            UCSizeStructure dstSizes = new UCSizeStructure();
            srcSizes.size0 = (ulong)src._imageCore.size0;
            srcSizes.size1 = (ulong)src._imageCore.size1;
            srcSizes.size2 = (ulong)src._imageCore.size2;
            srcSizes.size3 = (ulong)src._imageCore.size3;

            fixed (AVImageCore* srcptr = &src._imageCore)
            fixed (AVImageCore* dstptr = &dst._imageCore)
            {
                byte** srcd = (byte**)((void*)(srcptr->data0));
                byte** dstd = (byte**)((void*)(dstptr->data0));

                ulong* srcs = &(srcSizes.size0);
                ulong* dsts = &(dstSizes.size0);
                AVImgUtilsC.av_image_copy_uc_from(dstd, dsts, srcd, srcs, src.Format, src.Width, src.Height);
            }
            dst._imageCore.size0 = (int)dstSizes.size0;
            dst._imageCore.size1 = (int)dstSizes.size1;
            dst._imageCore.size2 = (int)dstSizes.size2;
            dst._imageCore.size3 = (int)dstSizes.size3;
        }
        public static void CopyImageUc(Frame src, Image dst)
        {
            UCSizeStructure srcSizes = new UCSizeStructure();
            UCSizeStructure dstSizes = new UCSizeStructure();
            srcSizes.size0 = (ulong)src._frame->linesize0;
            srcSizes.size1 = (ulong)src._frame->linesize1;
            srcSizes.size2 = (ulong)src._frame->linesize2;
            srcSizes.size3 = (ulong)src._frame->linesize3;

            fixed (AVImageCore* dstptr = &dst._imageCore)
            {
                byte** srcd = (byte**)((void*)(&src._frame->data0));
                byte** dstd = (byte**)((void*)(&dstptr->data0));

                ulong* srcs = &(srcSizes.size0);
                ulong* dsts = &(dstSizes.size0);
                AVImgUtilsC.av_image_copy_uc_from(dstd, dsts, srcd, srcs, (AVPixelFormat)src._frame->format, src._frame->width, src._frame->height);
            }
            dst._imageCore.size0 = (int)dstSizes.size0;
            dst._imageCore.size1 = (int)dstSizes.size1;
            dst._imageCore.size2 = (int)dstSizes.size2;
            dst._imageCore.size3 = (int)dstSizes.size3;
        }
    }
}
