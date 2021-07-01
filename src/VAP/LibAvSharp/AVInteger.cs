// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
namespace LibAvSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public struct AVInteger
    {
        [FieldOffset(0)]
        private ulong ulong0;

        [FieldOffset(8)]
        private ulong ulong1;

        [FieldOffset(0)]
        private ushort v0;

        [FieldOffset(2)]
        private ushort v1;

        [FieldOffset(4)]
        private ushort v2;

        [FieldOffset(6)]
        private ushort v3;

        [FieldOffset(8)]
        private ushort v4;

        [FieldOffset(10)]
        private ushort v5;

        [FieldOffset(12)]
        private ushort v6;

        [FieldOffset(14)]
        private ushort v7;

        public ushort this[int index]
        {
            get
            {
                return index switch
                {
                    0 => v0,
                    1 => v1,
                    2 => v2,
                    3 => v3,
                    4 => v4,
                    5 => v5,
                    6 => v6,
                    7 => v7,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        v0 = value;
                        break;
                    case 1:
                        v1 = value;
                        break;
                    case 2:
                        v2 = value;
                        break;
                    case 3:
                        v3 = value;
                        break;
                    case 4:
                        v4 = value;
                        break;
                    case 5:
                        v5 = value;
                        break;
                    case 6:
                        v6 = value;
                        break;
                    case 7:
                        v7 = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public AVInteger(ulong val)
        {
            ulong0 = ulong1 = v0 = v1 = v2 = v3 = v4 = v5 = v6 = v7 = 0;
            ulong0 = val;
        }

        public static AVInteger operator +(AVInteger a, AVInteger b) => IntegerC.av_add_i(a, b);
        public static AVInteger operator -(AVInteger a, AVInteger b) => IntegerC.av_sub_i(a, b);
        public static AVInteger operator *(AVInteger a, AVInteger b) => IntegerC.av_mul_i(a, b);
        public static AVInteger operator /(AVInteger a, AVInteger b) => IntegerC.av_div_i(a, b);
        public static AVInteger operator %(AVInteger a, AVInteger b)
        {
            AVInteger res = new AVInteger(0);
            return IntegerC.av_mod_i(ref res, a, b);
        }
        public static bool operator <(AVInteger a, AVInteger b) => IntegerC.av_cmp_i(a, b) < 0;
        public static bool operator <=(AVInteger a, AVInteger b) => IntegerC.av_cmp_i(a, b) <= 0;
        public static bool operator >(AVInteger a, AVInteger b) => IntegerC.av_cmp_i(a, b) > 0;
        public static bool operator >=(AVInteger a, AVInteger b) => IntegerC.av_cmp_i(a, b) >= 0;
        public static bool operator ==(AVInteger a, AVInteger b) => IntegerC.av_cmp_i(a, b) == 0;
        public static bool operator !=(AVInteger a, AVInteger b) => IntegerC.av_cmp_i(a, b) != 0;
        public static AVInteger operator <<(AVInteger a, int b) => IntegerC.av_shr_i(a, -b);
        public static AVInteger operator >>(AVInteger a, int b) => IntegerC.av_shr_i(a, b);
        public static explicit operator long(AVInteger a) => IntegerC.av_i2int(a);
        public static explicit operator AVInteger(long a) => IntegerC.av_int2i(a);
        public static int Log2(AVInteger a) => IntegerC.av_log2_i(a);
    }
}
