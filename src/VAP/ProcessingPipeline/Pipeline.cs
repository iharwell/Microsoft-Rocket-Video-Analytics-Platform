﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using DNNDetector.Model;
using OpenCvSharp;
using Utils.Items;

namespace ProcessingPipeline
{
    /// <summary>
    ///   The core processing pipeline structure.
    /// </summary>
    public class Pipeline
    {
        /// <summary>
        ///   The set of stages used to process a frame.
        /// </summary>
        private readonly IList<IProcessor> _stages = new List<IProcessor>();

        /// <summary>
        ///   Appends a stage to the end of the pipeline.
        /// </summary>
        /// <param name="stage">
        ///   The stage to add.
        /// </param>
        public void AppendStage(IProcessor stage) { _stages.Add(stage); }

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
            return items;
        }

        public IProcessor LastStage => _stages[_stages.Count - 1];

        public IProcessor this[int index]
        {
            get
            {
                return _stages[index];
            }
        }
        public int Count
        {
            get
            {
                return _stages.Count;
            }
        }
    }
}