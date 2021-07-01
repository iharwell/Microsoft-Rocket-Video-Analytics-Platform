// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibAvSharp
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AVRational
    {
        public int Numerator;
        public int Denominator;

        public AVRational(int num, int den)
        {
            Numerator = num;
            Denominator = den;
        }

        public static AVRational operator +(AVRational a, AVRational b) => RationalC.av_add_q(a, b);
        public static AVRational operator -(AVRational a, AVRational b) => RationalC.av_sub_q(a, b);
        public static AVRational operator *(AVRational a, AVRational b) => RationalC.av_mul_q(a, b);
        public static AVRational operator /(AVRational a, AVRational b) => RationalC.av_div_q(a, b);

        public AVRational Inverse() => RationalC.av_inv_q(this);
        public static AVRational GCD(AVRational a, AVRational b, int max_den, AVRational def) => RationalC.av_gdc_q(a, b, max_den, def);
        public static AVRational FromDouble(double d, int max) => RationalC.av_d2q(d, max);

        public static explicit operator double(AVRational r) => r.Numerator / (double)r.Denominator;
    }
}
