// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using DNNDetector.Model;
using OpenCvSharp;
using Utils.Items;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace ProcessingPipeline
{
    /// <summary>
    ///   The core processing pipeline structure.
    /// </summary>
    public class Pipeline
    {
        ConcurrentQueue<FramePacket> inputQueue;
        ConcurrentQueue<FramePacket> outputBuffer;

        private List<ConcurrentQueue<FramePacket>> buffers;
        private List<Task> procBlocks;

        public Pipeline()
        {
            DataflowBlockOptions opts = new DataflowBlockOptions
            {
                BoundedCapacity = 60,
                EnsureOrdered = true
            };
            inputQueue = new ConcurrentQueue<FramePacket>();
            outputBuffer = new ConcurrentQueue<FramePacket>();
            _stages = new List<IProcessor>();
            buffers = new List<ConcurrentQueue<FramePacket>>();
            procBlocks = new List<Task>();
        }

        private bool? de = null;

        private bool DisplayEnabled
        {
            get
            {
                if (de == null)
                {
                    de = (from IProcessor stage in _stages
                          where stage.DisplayOutput
                          select 1).Any();
                }
                return de.Value;
            }
        }

        /// <summary>
        ///   The set of stages used to process a frame.
        /// </summary>
        private readonly IList<IProcessor> _stages;

        /// <summary>
        ///   Appends a stage to the end of the pipeline.
        /// </summary>
        /// <param name="stage">
        ///   The stage to add.
        /// </param>
        public void AppendStage(IProcessor stage) { _stages.Add(stage); }

        protected class FramePacket
        {
            public IFrame frame { get; set; }
            public IList<IFramedItem> items { get; set; }
            public IProcessor prev { get; set; }
        }

        public void SyncPostFrame(IFrame frame)
        {
            FramePacket packet = new FramePacket
            {
                frame = frame,
                items = new List<IFramedItem>(),
                prev = null
            };

            while (inputQueue.Count >= 50)
            {
                System.Threading.Thread.Sleep(1);
            }
            inputQueue.Enqueue(packet);
        }

        public void SpoolPipeline()
        {
            procBlocks.Clear();
            for (int i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                ConcurrentQueue<FramePacket> inQueue;
                ConcurrentQueue<FramePacket> outQueue;
                if (i == 0)
                {
                    inQueue = inputQueue;
                }
                else
                {
                    inQueue = buffers[^1];
                }

                if (i == _stages.Count - 1)
                {
                    outQueue = outputBuffer;
                }
                else
                {
                    outQueue = new ConcurrentQueue<FramePacket>();
                    buffers.Add(outQueue);
                }

                Task stageTask = new Task(
                    () =>
                    {
                        while (true)
                        {
                            if (inQueue.TryDequeue(out var packet))
                            {
                                var f = packet.frame;
                                var items = packet.items;
                                var prev = packet.prev;
                                stage.Run(f, ref items, prev);
                                FramePacket outPacket = new FramePacket()
                                {
                                    frame = f,
                                    items = items,
                                    prev = prev
                                };
                                while (outQueue.Count >= 200)
                                {
                                    Thread.Sleep(15);
                                }
                                outQueue.Enqueue(outPacket);
                            }
                        }
                    });
                stageTask.Start();
                procBlocks.Add(stageTask);
            }
        }

        public bool TryReceiveList(out IList<IFramedItem> items, out IFrame frameout)
        {
            if (outputBuffer.TryDequeue(out var packet))
            {
                frameout = packet.frame;
                items = packet.items;
                return true;
            }
            frameout = null;
            items = null;
            return false;
        }
        /// <summary>
        ///   Processes a frame using the processors in this pipeline.
        /// </summary>
        /// <param name="frame">
        ///   The frame to process.
        /// </param>
        /// <returns>
        ///   Returns all items found in the frame.
        /// </returns>
        public IList<IFramedItem> ProcessFrame(IFrame frame)
        {
            IList<IFramedItem> items = new List<IFramedItem>();
            IProcessor prev = null;
            foreach (IProcessor stage in _stages)
            {
                stage.Run(frame, ref items, prev);
                prev = stage;
            }
            if (DisplayEnabled)
            {
                Cv2.WaitKey(1);
            }
            return items;
        }

        /// <summary>
        /// The last stage in this pipeline.
        /// </summary>
        public IProcessor LastStage => _stages[_stages.Count - 1];

        /// <summary>
        ///   Gets the <see cref="IProcessor"/> stage at the provide index.
        /// </summary>
        /// <param name="index">
        ///   The index of the stage to get.
        /// </param>
        /// <returns>
        ///   Returns the <see cref="IProcessor"/> at the provided index.
        /// </returns>
        public IProcessor this[int index]
        {
            get
            {
                return _stages[index];
            }
        }

        /// <summary>
        ///   The number of stages in this <see cref="Pipeline"/>.
        /// </summary>
        public int Count
        {
            get
            {
                return _stages.Count;
            }
        }
    }
}
