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
    [KnownType( typeof( FramedItem ) )]
    [KnownType( typeof( ItemID ) )]
    [KnownType( typeof( LineTriggeredItemID ) )]
    [KnownType( typeof( Frame ) )]
    [KnownType( typeof( OpenCvSharp.Mat ) )]
    [KnownType( typeof( List<IItemID> ) )]
    public class ItemPath : IItemPath
    {
        public ItemPath()
        {
            FramedItems = new List<IFramedItem>();
            highestConfidenceFrame = -1;
            highestConfidenceID = -1;
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
                if ( highestConfidenceFrame == -1 )
                {
                    UpdateHighestConfidence();
                }
                return highestConfidenceFrame;
            }
        }

        protected int HighestConfidenceIDIndex
        {
            get
            {
                if ( highestConfidenceID == -1 )
                {
                    UpdateHighestConfidence();
                }
                return highestConfidenceID;
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
        private int highestConfidenceFrame;

        [IgnoreDataMember]
        private int highestConfidenceID;

        [IgnoreDataMember]
        public int Count { get; set; }

        private void UpdateHighestConfidence()
        {
            double max = -1;
            int frameNum = -1;
            int idNum = -1;
            for ( int i = 0; i < FramedItems.Count; i++ )
            {
                for ( int j = 0; j < FramedItems[i].ItemIDs.Count; j++ )
                {
                    if( FramedItems[i].ItemIDs[j].Confidence > max )
                    {
                        max = FramedItems[i].ItemIDs[j].Confidence;
                        frameNum = i;
                        idNum = j;
                    }
                }
            }

            highestConfidenceFrame = frameNum;
            highestConfidenceID = idNum;
        }

        /// <inheritdoc />
        IList<IFramedItem> IItemPath.FramedItems => FramedItems;
    }
}
