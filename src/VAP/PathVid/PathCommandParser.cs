// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.Items;

namespace PathVid
{
    public class PathCommandParser
    {
        private class FileSlice
        {
            public FileSlice(string fileName, int start, int end, float frameRate)
            {
                FileName = fileName;
                FrameRate = frameRate;
                StartIndex = start;
                EndIndex = end;
            }
            public string FileName { get; set; }
            public float FrameRate { get; private set; }
            public long PrevKeyFrame { get; set; }
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }

            public TimeSpan StartOffset
            {
                get
                {
                    return IndexToTimeStamp(StartIndex, FrameRate);
                }
            }
        }

        public string FFMPEGPath { get; set; }

        public string WorkingPath { get; set; }

        public string OutputFormat { get; set; }

        public Func<string, DateTime> FileToTimeStampParser { get; set; }

        public void BuildVideo(IItemPath path, int index)
        {
            var segments = GetContinuousSegments(path);
            var files = MakeSlices(segments);

            MakeConcatFile(files);
            string outname = "\\" + GetPathFileName(path) + ".mp4";
            /*if (System.IO.File.Exists(outname))
            {
                System.IO.File.Delete(outname);
            }
            while (System.IO.File.Exists(outname))
            {
                Thread.Sleep(10);
            }*/
            string command = "-f concat -safe 0 -i \"" + WorkingPath + "\\concat.txt\"" + " -c copy \"" + outname + "\" -y";
            var proc = System.Diagnostics.Process.Start(FFMPEGPath, command);
            proc.WaitForExit();
        }

        private void MakeConcatFile(IList<string> files)
        {
            string txtPath = WorkingPath + "\\concat.txt";
            var txt = new StreamWriter(txtPath, false);

            for (int i = 0; i < files.Count; i++)
            {
                string fileName = files[i].Replace("\\", "/");

                txt.WriteLine("file " + fileName);
            }
            txt.Flush();
            txt.Close();
            txt.Dispose();

        }

        public void MakeSlices(IItemPath path)
        {
            var segments = GetContinuousSegments(path);
            MakeSlices(segments);
        }

        private IList<string> MakeSlices(IList<FileSlice> slices)
        {
            List<string> files = new();

            for (int i = 0; i < slices.Count; i++)
            {
                string outname = WorkingPath + "\\slice" + i + ".mp4";

                /*if (System.IO.File.Exists(outname))
                {
                    System.IO.File.Delete(outname);
                }
                while (System.IO.File.Exists(outname))
                {
                    Thread.Sleep(10);
                }*/

                string inName = "\"" + slices[i].FileName + "\"";
                var startTime = IndexToTimeStamp(slices[i].StartIndex, slices[i].FrameRate);
                var endTime = IndexToTimeStamp(slices[i].EndIndex, slices[i].FrameRate);

                int startOffset = (int)slices[i].PrevKeyFrame;

                string command =$"-ss {(int)(startTime.TotalSeconds)} -to {(int)(endTime.TotalSeconds + 1)} -c:v hevc_cuvid " + this.InputFile(inName) + " " + this.SelectFrames(slices[i].StartIndex-startOffset, slices[i].EndIndex - startOffset) + " -c:v libx265 " + outname + " -y";
                var proc = System.Diagnostics.Process.Start(FFMPEGPath, command);
                proc.WaitForExit();
                files.Add(outname);
            }
            return files;
        }


        private IList<FileSlice> GetContinuousSegments(IItemPath path)
        {
            if (IsPathOneFile(path))
            {
                return GetSingleFileSlices(this.SortedItems(path.FramedItems));
            }
            else
            {
                List<FileSlice> slices = new();
                var groups = GroupedAndSortedItems(path);

                foreach (var group in groups)
                {
                    var items = group.ToList();
                    var fileSlices = GetSingleFileSlices(items);
                    for (int i = 0; i < fileSlices.Count; i++)
                    {
                        slices.Add(fileSlices[i]);
                    }
                }
                return slices;
            }

        }

        private IList<FileSlice> GetSingleFileSlices(IList<IFramedItem> items)
        {
            List<FileSlice> slices = new();
            if (IsPathSimple(items))
            {
                var bounds = GetSimpleFileBounds(items);
                var file = items[0].Frame.SourceName;
                var slice = new FileSlice(file, bounds.startIndex, bounds.endIndex, items[0].Frame.FrameRate)
                {
                    PrevKeyFrame = bounds.lastKeyFrame
                };
                slices.Add(slice);
                return slices;
            }
            else
            {
                int segmentStart = items[0].Frame.FileFrameIndex;
                int segmentEnd = items[0].Frame.FileFrameIndex;
                int lastKeyFrame = GetLastKeyFrame(items[0].Frame);
                float frameRate = items[0].Frame.FrameRate;
                var file = items[0].Frame.SourceName;

                for (int i = 0; i < items.Count; i++)
                {
                    segmentStart = items[i].Frame.FileFrameIndex;
                    segmentEnd = items[i].Frame.FileFrameIndex;
                    lastKeyFrame = GetLastKeyFrame(items[i].Frame);

                    int j;
                    for (j = i + 1; j < items.Count && (j - i == items[j].Frame.FileFrameIndex - items[i].Frame.FileFrameIndex); j++)
                    {
                        var jFrame = items[j].Frame;
                        if (jFrame.FileFrameIndex < segmentStart)
                        {
                            segmentStart = jFrame.FileFrameIndex;
                            lastKeyFrame = GetLastKeyFrame(jFrame);
                        }
                        segmentEnd = Math.Max(segmentEnd, jFrame.FileFrameIndex);
                    }
                    i = j - 1;
                    var slice = new FileSlice(file, segmentStart, segmentEnd, frameRate)
                    {
                        PrevKeyFrame = lastKeyFrame
                    };
                    slices.Add(slice);
                }
                return slices;
            }
        }

        private IList<IList<IFramedItem>> GroupedAndSortedItems(IItemPath path)
        {
            var v = from item in path.FramedItems
                    let info = new
                    {
                        Source = item.Frame.SourceName,
                        SourceFps = item.Frame.FrameRate,
                        FileIndex = item.Frame.FileFrameIndex,
                        Item = item
                    }
                    orderby info.FileIndex ascending
                    group info by info.Source into newGroup
                    let firstInGroup = newGroup.First()
                    orderby FileToTimeStampParser(newGroup.Key) + IndexToTimeStamp(firstInGroup.FileIndex, firstInGroup.SourceFps)
                    select newGroup;

            List<IList<IFramedItem>> results = new();
            foreach(var group in v)
            {
                List<IFramedItem> groupList = new();
                foreach(var entry in group)
                {
                    groupList.Add(entry.Item);
                }
                results.Add(groupList);
            }
            return results;
        }
        private IList<IFramedItem> SortedItems(IItemPath path)
        {
            return SortedItems(path.FramedItems);
        }
        private IList<IFramedItem> SortedItems(IList<IFramedItem> segment)
        {
            return (from item in segment
                    let index = item.Frame.FrameIndex
                    orderby index ascending
                    select item).ToList();
        }

        private bool IsPathSimple(IItemPath path)
        {
            return IsPathSimple(path.FramedItems);
        }

        private bool IsPathSimple(IList<IFramedItem> items)
        {
            var fis = items;

            string file = fis[0].Frame.SourceName;
            int prevFrame = fis[0].Frame.FileFrameIndex;
            for (int i = 1; i < fis.Count; i++)
            {
                if (fis[i].Frame.FileFrameIndex != prevFrame + 1 || fis[i].Frame.SourceName.CompareTo(file) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsPathOneFile(IItemPath path)
        {
            var fis = path.FramedItems;

            string file = fis[0].Frame.SourceName;
            for (int i = 1; i < fis.Count; i++)
            {
                if (fis[i].Frame.SourceName.CompareTo(file) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        private string SelectFrames(int startFrame, int endFrame)
        {
            // select <expr> <outputs>
            return $"-vf select=\"between(n\\,{startFrame}\\,{endFrame}),setpts=PTS-STARTPTS\"";
        }

        private string MakeCutCommand(string sourceFile, string destFile, int startFrame, int endFrame, float fps)
        {
            return "ffmpeg " + SeekTo(startFrame, fps) + " " + InputFile(sourceFile) + " " + StopAt(endFrame, fps) + "-c copy" + destFile;
        }

        private string StopAt(int endFrame, float fps)
        {
            return $"-to {IndexToTimeString(endFrame, fps)}";
        }

        private string InputFile(string sourceFile)
        {
            return $"-i {sourceFile}";
        }

        private string SeekTo(int index, float fps)
        {
            return $"-ss {IndexToTimeString(index, fps)}";
        }

        private static TimeSpan IndexToTimeStamp(int index, float fps)
        {
            return TimeSpan.FromSeconds(index / fps);
        }

        private static string IndexToTimeString(int index, float fps)
        {
            TimeSpan time = IndexToTimeStamp(index, fps);
            return time.TotalSeconds.ToString();
        }

        private (int startIndex, int endIndex, int lastKeyFrame) GetSimpleFileBounds(IItemPath path)
        {
            return GetSimpleFileBounds(path.FramedItems);
        }

        private (int startIndex, int endIndex, int lastKeyFrame) GetSimpleFileBounds(IList<IFramedItem> items)
        {
            int minFrame = int.MaxValue;
            int maxFrame = int.MinValue;
            int lastKeyFrame = 0;
            for (int i = 0; i < items.Count; i++)
            {
                int frame = items[i].Frame.FileFrameIndex;
                if (minFrame > frame)
                {
                    minFrame = frame;
                    lastKeyFrame = GetLastKeyFrame(items[i].Frame);
                }
                maxFrame = Math.Max(maxFrame, frame);
            }
            return (minFrame, maxFrame, lastKeyFrame);
        }

        private int GetLastKeyFrame(IFrame frame)
        {
            if (frame is Frame f)
            {
                return (int)f.LastKeyFrame;
            }
            else
            {
                return (int)((int)(frame.FileFrameIndex / frame.FrameRate) * frame.FrameRate);
            }
        }

        private string GetPathFileName(IItemPath path)
        {
            IFramedItem earliestID = null;
            IFramedItem latestID = null;
            IFramedItem bestID = null;
            for (int i = 0; i < path.FramedItems.Count; i++)
            {
                var fi = path.FramedItems[i];
                if (earliestID is null || earliestID.Frame.FrameIndex > fi.Frame.FrameIndex)
                {
                    earliestID = fi;
                }
                if (latestID is null || latestID.Frame.FrameIndex < fi.Frame.FrameIndex)
                {
                    latestID = fi;
                }
                if (bestID is null || bestID.ItemIDs[bestID.HighestConfidenceIndex].Confidence < fi.ItemIDs[fi.HighestConfidenceIndex].Confidence)
                {
                    bestID = fi;
                }
            }

            var timeStamp = earliestID.Frame.TimeStamp;
            var timeCode1 = $"{timeStamp.Year}{timeStamp.Month.ToString("D2")}{timeStamp.Day.ToString("D2")}{timeStamp.Hour.ToString("D2")}{timeStamp.Minute.ToString("D2")}{timeStamp.Second.ToString("D2")}";

            timeStamp = latestID.Frame.TimeStamp;
            var timeCode2 = $"{timeStamp.Year}{timeStamp.Month.ToString("D2")}{timeStamp.Day.ToString("D2")}{timeStamp.Hour.ToString("D2")}{timeStamp.Minute.ToString("D2")}{timeStamp.Second.ToString("D2")}";

            return $"{timeCode1}-{timeCode2}-{bestID.ItemIDs[bestID.HighestConfidenceIndex].ObjName}";
        }
    }
}
