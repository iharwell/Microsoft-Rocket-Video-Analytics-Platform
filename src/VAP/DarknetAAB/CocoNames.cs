// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarknetAAB
{
    public static class Coco
    {
        private static string[] _names;
        public static string[] Names
        {
            get
            {
                if (_names == null)
                {
                    MemoryStream ms = new MemoryStream(Properties.Resources.coco);
                    StreamReader reader = new StreamReader(ms);
                    string? line = reader.ReadLine();
                    List<string> lines = new List<string>();
                    while (line != null)
                    {
                        lines.Add(line);
                        line = reader.ReadLine();
                    }
                    reader.Close();
                    _names = lines.ToArray();
                }
                return _names;
            }
            set
            {
                _names = value;
            }
        }
    }
}
