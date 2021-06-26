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
        ulong ulong0;

        [FieldOffset(8)]
        ulong ulong1;

        [FieldOffset(0)]
        ushort v0;

        [FieldOffset(2)]
        ushort v1;

        [FieldOffset(4)]
        ushort v2;

        [FieldOffset(6)]
        ushort v3;

        [FieldOffset(8)]
        ushort v4;

        [FieldOffset(10)]
        ushort v5;

        [FieldOffset(12)]
        ushort v6;

        [FieldOffset(14)]
        ushort v7;

        public ushort this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return v0;
                    case 1:
                        return v1;
                    case 2:
                        return v2;
                    case 3:
                        return v3;
                    case 4:
                        return v4;
                    case 5:
                        return v5;
                    case 6:
                        return v6;
                    case 7:
                        return v7;
                    default:
                        throw new IndexOutOfRangeException();
                }
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

        public AVInteger( ulong val )
        {
            ulong0 = ulong1 = v0 = v1 = v2 = v3 = v4 = v5 = v6 = v7 = 0;
            ulong0 = val;
        }

        public static AVInteger operator +( AVInteger a, AVInteger b ) => IntegerC.av_add_i(a, b);
        public static AVInteger operator -( AVInteger a, AVInteger b ) => IntegerC.av_sub_i(a, b);
        public static AVInteger operator *( AVInteger a, AVInteger b ) => IntegerC.av_mul_i(a, b);
        public static AVInteger operator /( AVInteger a, AVInteger b ) => IntegerC.av_div_i(a, b);
        public static AVInteger operator %( AVInteger a, AVInteger b )
        {
            AVInteger res = new AVInteger(0);
            return IntegerC.av_mod_i(ref res, a, b);
        }
        public static bool operator <( AVInteger a, AVInteger b ) => IntegerC.av_cmp_i(a, b) < 0;
        public static bool operator <=( AVInteger a, AVInteger b ) => IntegerC.av_cmp_i(a, b) <= 0;
        public static bool operator >( AVInteger a, AVInteger b ) => IntegerC.av_cmp_i(a, b) > 0;
        public static bool operator >=( AVInteger a, AVInteger b ) => IntegerC.av_cmp_i(a, b) >= 0;
        public static bool operator ==( AVInteger a, AVInteger b ) => IntegerC.av_cmp_i(a, b) == 0;
        public static bool operator !=( AVInteger a, AVInteger b ) => IntegerC.av_cmp_i(a, b) != 0;
        public static AVInteger operator <<( AVInteger a, int b ) => IntegerC.av_shr_i(a, -b);
        public static AVInteger operator >>( AVInteger a, int b ) => IntegerC.av_shr_i(a, b);
        public static explicit operator long( AVInteger a ) => IntegerC.av_i2int(a);
        public static explicit operator AVInteger( long a ) => IntegerC.av_int2i(a);
        public static int Log2( AVInteger a ) => IntegerC.av_log2_i(a);
    }
}
