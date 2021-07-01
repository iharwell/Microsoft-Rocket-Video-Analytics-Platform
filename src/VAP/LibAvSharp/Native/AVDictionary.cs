// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    public unsafe struct AVDictionary
    {
        public int count;
        public AVDictionaryEntry* elems;
    }
    public unsafe struct AVDictionaryEntry
    {
        public byte* key;
        public byte* value;
    }
}
