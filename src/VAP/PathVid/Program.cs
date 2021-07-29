using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Utils.Items;

namespace PathVid
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0 || args[0].Contains('?'))
            {
                Console.WriteLine("Usage: <exe> <path to XML files>");
            }
            var pathLists = new List<IList<IItemPath>>();
            for (int i = 0; i < args.Length; i++)
            {
                var paths = ReadXML(args[i]);
                pathLists.Add(paths);
            }

            PathCommandParser parser = new();
            parser.FFMPEGPath = "C:\\Users\\thene\\source\\repos\\ffmpeg\\bin\\ffmpeg.exe";
            parser.FileToTimeStampParser = TimeStampParser;
            parser.WorkingPath = "F:\\WorkingPath";

            for (int i = 0; i < pathLists.Count; i++)
            {
                for (int j = 0; j < pathLists[i].Count; j++)
                {
                    parser.BuildVideo(pathLists[i][j], i);

                }
            }

        }

        internal static DateTime TimeStampParser(string f)
        {
            DateTime ts = default;

            if (f == null)
            {
                return ts;
            }

            var parts = f.Split('_');
            string startString = parts[2];

            if (parts.Length != 5 || startString.Length != 14)
            {
                return ts;
            }

            int year = int.Parse(startString.Substring(0, 4));
            int month = int.Parse(startString.Substring(4, 2));
            int day = int.Parse(startString.Substring(6, 2));
            int hour = int.Parse(startString.Substring(8, 2));
            int minute = int.Parse(startString.Substring(10, 2));
            int second = int.Parse(startString.Substring(12, 2));

            return new DateTime(year, month, day, hour, minute, second);
        }

        private static IList<IItemPath> ReadXML(string path)
        {
            List<IItemPath> paths = new();

            DataContractSerializer reader = new DataContractSerializer(typeof(ItemPath));

            var files = Directory.GetFiles(path, "*.xml");

            foreach(var fname in files)
            {
                var file = new FileStream(fname, FileMode.Open, FileAccess.Read);
                var result = reader.ReadObject(file);
                if( result != null && result is IItemPath ipath)
                {
                    paths.Add(ipath);
                }
                file.Close();
                file.Dispose();
            }

            return paths;
        }

    }
}
