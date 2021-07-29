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
using System.Runtime.Serialization;

namespace ProcessingPipeline
{
    /// <summary>
    ///   The core processing pipeline structure.
    /// </summary>
    [DataContract]
    [KnownType(typeof(List<IProcessor>))]
    [KnownType(typeof(FallingEdgeTrigger))]
    [KnownType(typeof(HeavyDarknetProcessor))]
    [KnownType(typeof(LightDarknetProcessor))]
    [KnownType(typeof(LineCrossingItemSearchProcessor))]
    [KnownType(typeof(LineCrossingProcessor))]
    [KnownType(typeof(PreProcessorStage))]
    [KnownType(typeof(System.Drawing.Color))]
    [KnownType(typeof(SimpleDNNProcessor))]
    public class Pipeline : IDisposable
    {
        /// <summary>
        ///   The set of stages used to process a frame.
        /// </summary>
        [DataMember]
        private readonly IList<IProcessor> _stages;

        private bool _disposedValue;
        private List<ConcurrentQueue<FramePacket>> buffers;
        private bool? de = null;
        ConcurrentQueue<FramePacket> inputQueue;
        ConcurrentQueue<FramePacket> outputBuffer;
        private List<Task> procBlocks;
        private volatile int inputCount;
        private volatile bool running;

        public Pipeline()
        {
            inputQueue = new ConcurrentQueue<FramePacket>();
            outputBuffer = new ConcurrentQueue<FramePacket>();
            _stages = new List<IProcessor>();
            buffers = new List<ConcurrentQueue<FramePacket>>();
            procBlocks = new List<Task>();
        }
        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Pipeline()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
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

        /// <summary>
        /// The last stage in this pipeline.
        /// </summary>
        public IProcessor LastStage => _stages[_stages.Count - 1];

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
        ///   Appends a stage to the end of the pipeline.
        /// </summary>
        /// <param name="stage">
        ///   The stage to add.
        /// </param>
        public void AppendStage(IProcessor stage) { _stages.Add(stage); }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
            if (DisplayEnabled && (frame.FrameIndex & 3) == 0)
            {
                Cv2.WaitKey(1);
            }
            return items;
        }

        public bool StillProcessing
        {
            get
            {
                if( inputQueue.Any() || outputBuffer.Any())
                {
                    return true;
                }
                for (int i = 0; i < this.buffers.Count; i++)
                {
                    if(buffers[i].Any())
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void SpoolPipeline()
        {
            procBlocks.Clear();
            Task procTask = new Task(() =>
            {
                /*List<FramePacket> inputs = new List<FramePacket>();
                ConcurrentBag<FramePacket> outputBag = new();*/
                bool notified = false;
                int loopsUntilCountCheck = 0;
                while (running)
                {
                    /*bool keepChecking = true;
                    for (int i = 0; i < 4 && keepChecking; i++)
                    {
                        if (inputQueue.TryDequeue(out var result))
                        {
                            inputs.Add(result);
                        }
                        else
                        {
                            keepChecking = false;
                        }
                    }

                    if (inputs.Count == 0)
                    {
                        Thread.Sleep(5);
                        continue;
                    }

                    while (outputBuffer.Count > 50)
                    {
                        Thread.Sleep(5);
                    }
                    Parallel.ForEach(inputs, (FramePacket packet) =>
                    {
                        FramePacket outPacket = new FramePacket();
                        outPacket.frame = packet.frame;
                        outPacket.items = ProcessFrame(packet.frame);

                        outputBag.Add(outPacket);
                    });

                    List<FramePacket> outputs = outputBag.ToList();
                    outputs.Sort((FramePacket a, FramePacket b) => a.frame.FrameIndex - b.frame.FrameIndex);
                    for (int i = 0; i < outputs.Count; i++)
                    {
                        outputBuffer.Enqueue(outputs[i]);
                    }*/

                    if (inputQueue.TryDequeue(out var packet))
                    {
                        --inputCount;
                        var f = packet.frame;
                        if( loopsUntilCountCheck<=0)
                        {
                            while (outputBuffer.Count > 750)
                            {
                                if(notified)
                                {
                                    Thread.Sleep(0);
                                }
                                else
                                {
                                    Console.WriteLine("\tOutput Buffer Full");
                                    notified = true;
                                }
                            }
                            loopsUntilCountCheck = 750 - outputBuffer.Count;
                        }
                        else
                        {
                            --loopsUntilCountCheck;
                        }
                        notified = false;
                        FramePacket outPacket = new FramePacket();
                        outPacket.frame = f;
                        outPacket.items = ProcessFrame(f);

                        outputBuffer.Enqueue(outPacket);
                    }/**/
                }
            });
            running = true;
            procTask.Start();
            procBlocks.Add(procTask);
            /*for (int i = 0; i < _stages.Count; i++)
            {
                running = true;
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
                        while (running)
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
            }*/
        }

        public void SyncPostFrame(IFrame frame)
        {
            FramePacket packet = new FramePacket
            {
                frame = frame,
                items = new List<IFramedItem>(),
                prev = null
            };

            while (inputCount >= 50)
            {
                Thread.Sleep(10);
            }
            inputQueue.Enqueue(packet);
            inputCount++;
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

        public void HaltPipeline()
        {
            this.running = false;
            for (int i = 0; i < procBlocks.Count; i++)
            {
                while (!procBlocks[i].IsCompleted)
                {
                    Thread.Sleep(1);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    //inputQueue = null;
                    //outputBuffer = null;

                }
                for (int i = 0; i < _stages.Count; i++)
                {
                    if(running)
                    {
                        HaltPipeline();
                    }
                    if (_stages[i] is IDisposable d)
                    {
                        d.Dispose();
                    }
                    //_stages[i] = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        protected class FramePacket
        {
            public IFrame frame { get; set; }
            public IList<IFramedItem> items { get; set; }
            public IProcessor prev { get; set; }
        }
    }
}
