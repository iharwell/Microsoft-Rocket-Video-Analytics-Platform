using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAvSharp.Native
{
    unsafe public struct AVDictionary
    {
        public int count;
        public AVDictionaryEntry* elems;
    }
    unsafe public struct AVDictionaryEntry
    {
        public byte* key;
        public byte* value;
    }
}
