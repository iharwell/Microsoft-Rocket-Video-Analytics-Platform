// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text;
using OpenCvSharp;

namespace Utils.Items
{
    /// <summary>
    ///   A simple base class for <see cref="IItemPath" /> implementations providing default functionality.
    /// </summary>

    [Serializable]
    [KnownType(typeof(FramedItem))]
    [KnownType(typeof(ItemID))]
    [KnownType(typeof(LineTriggeredItemID))]
    [KnownType(typeof(Frame))]
    [KnownType(typeof(OpenCvSharp.Mat))]
    [KnownType(typeof(List<IItemID>))]
    public class ItemPath : IItemPath
    {
        public ItemPath()
        {
            FramedItems = new List<IFramedItem>();
            _highestConfidenceFrame = -1;
            _highestConfidenceID = -1;
        }

        /// <inheritdoc />
        public virtual int FrameIndex(int entryIndex)
        {
            return FramedItems[entryIndex].Frame.FrameIndex;
        }

        /// <inheritdoc cref="IItemPath.FramedItems" />
        public IList<IFramedItem> FramedItems { get; protected set; }
        public DateTime TimeStamp { get; }
        public string Category { get; }
        public double Confidence { get; }

        protected int HighestConfidenceFrameIndex
        {
            get
            {
                if (_highestConfidenceFrame == -1)
                {
                    UpdateHighestConfidence();
                }
                return _highestConfidenceFrame;
            }
        }

        protected int HighestConfidenceIDIndex
        {
            get
            {
                if (_highestConfidenceID == -1)
                {
                    UpdateHighestConfidence();
                }
                return _highestConfidenceID;
            }
        }

        [IgnoreDataMember]
        public IFramedItem HighestConfidenceFrame
        {
            get
            {
                return FramedItems[HighestConfidenceFrameIndex];
            }
        }

        [IgnoreDataMember]
        public Mat HighestConfidenceFrameData
        {
            get
            {
                return FramedItems[HighestConfidenceFrameIndex].Frame.FrameData;
            }
        }

        [IgnoreDataMember]
        public IItemID HighestConfidenceID
        {
            get
            {
                return HighestConfidenceFrame.ItemIDs[HighestConfidenceIDIndex];
            }
        }

        [IgnoreDataMember]
        private int _highestConfidenceFrame;

        [IgnoreDataMember]
        private int _highestConfidenceID;

        [IgnoreDataMember]
        public int Count { get; set; }

        private void UpdateHighestConfidence()
        {
            double max = -1;
            int frameNum = -1;
            int idNum = -1;
            for (int i = 0; i < FramedItems.Count; i++)
            {
                for (int j = 0; j < FramedItems[i].ItemIDs.Count; j++)
                {
                    if (FramedItems[i].ItemIDs[j].Confidence > max)
                    {
                        max = FramedItems[i].ItemIDs[j].Confidence;
                        frameNum = i;
                        idNum = j;
                    }
                }
            }

            _highestConfidenceFrame = frameNum;
            _highestConfidenceID = idNum;
        }

        /// <inheritdoc />
        IList<IFramedItem> IItemPath.FramedItems => FramedItems;
    }
}
