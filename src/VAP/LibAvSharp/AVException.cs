// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp
{
    public class AVException : Exception
    {
        public AVException()
            : base()
        { }
        public AVException(string? message)
            : base(message)
        { }
        public AVException(string? message, Exception? innerException)
            : base(message, innerException)
        { }
        public AVException(int errorCode)
            : base()
        {
            ErrorCode = errorCode;
        }
        public AVException(int errorCode, string? message)
            : base(message)
        {
            ErrorCode = errorCode;
        }
        public AVException(int errorCode, string? message, Exception? innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
        public int ErrorCode { get; set; }
        public string ErrorText
        {
            get
            {
                char[] chars = new char[]
                {
                    (char)(((-ErrorCode) >> 00)&0xFF),
                    (char)(((-ErrorCode) >> 08)&0xFF),
                    (char)(((-ErrorCode) >> 16)&0xFF),
                    (char)(((-ErrorCode) >> 24)&0xFF)
                };
                return new string(chars);
            }
        }

        public static void ProcessException(int errorCode, string errorMessage)
        {
            if (errorCode < 0)
            {
                throw new AVException(errorCode, errorMessage);
            }
        }

        public static void ProcessException(int errorCode)
        {
            if (errorCode < 0)
            {
                throw new AVException(errorCode);
            }
        }
    }
}
