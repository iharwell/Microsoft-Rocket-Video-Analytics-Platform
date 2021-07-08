// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;

namespace MotionTracker
{
    public class TimeCompressor
    {

        public float VelocityThreshold { get; set; }

        public int MaxJump { get; set; }

        public int BlockSize { get; set; }

        public TimeCompressor()
            : this(7.0f, 15, 30)
        {}

        public TimeCompressor(float velocityThreshold, int maxJump, int blockSize)
        {
            VelocityThreshold = velocityThreshold;
            MaxJump = maxJump;
            BlockSize = blockSize;
        }

        public void ProcessPath(IItemPath path)
        {
            List<(int startIndex, int endIndex, float velocity)> rangeSpeeds = new();
            List<IFramedItem> sortedPath = new(path.FramedItems);
            sortedPath.Sort((IFramedItem a, IFramedItem b) => a.Frame.FrameIndex - b.Frame.FrameIndex);
            for (int i = 0; i < sortedPath.Count; i+=BlockSize)
            {
                int end = i;

                int startFrame = sortedPath[i].Frame.FrameIndex;
                for (; end < i+BlockSize && end<sortedPath.Count; ++end)
                {
                    if(sortedPath[end].Frame.FrameIndex>=BlockSize+startFrame)
                    {
                        --end;
                        break;
                    }
                }

                var v = Motion.PathVelocity(path.FramedItems, i, end);

                if (v < VelocityThreshold)
                {
                    rangeSpeeds.Add((i, end, v));
                }
            }

            for (int i = rangeSpeeds.Count-1; i >= 0; --i)
            {
                var entry = rangeSpeeds[i];
                int prevFrame = sortedPath[entry.startIndex].Frame.FrameIndex;
                int startFrame = prevFrame;
                int endFrame = sortedPath[entry.startIndex].Frame.FrameIndex;
                for (int j = entry.startIndex; j <= rangeSpeeds[i].endIndex && sortedPath[j].Frame.FrameIndex < endFrame; j++)
                {
                    int currentFrame = sortedPath[j].Frame.FrameIndex;

                    while (currentFrame<=prevFrame+MaxJump)
                    {
                        sortedPath.RemoveAt(j);
                        if(j>sortedPath.Count)
                        {
                            break;
                        }
                        currentFrame = sortedPath[j].Frame.FrameIndex;
                    }
                }
            }
            path.FramedItems.Clear();
            for (int i = 0; i < sortedPath.Count; i++)
            {
                path.FramedItems.Add(sortedPath[i]);
            }
        }
    }
}
