// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    [DataContract]
    [KnownType(typeof(Dictionary<string, LineSegment>))]
    [KnownType(typeof(HashSet<string>))]
    public class LineCrossingItemSearchProcessor : IProcessor
    {
        private List<IList<IFramedItem>> _itemBuffer;

        public LineCrossingItemSearchProcessor()
        {
            _itemBuffer = new List<IList<IFramedItem>>();
            BufferSize = 150;
        }

        [DataMember]
        public Color BoundingBoxColor { get; set; }
        [DataMember]
        public int BufferSize { get; set; }
        [DataMember]
        public HashSet<string> IncludeCategories { get; set; }
        /// <inheritdoc/>
        [DataMember]
        public HashSet<string> ExcludeCategories { get; set; }
        [DataMember]
        public bool DisplayOutput { get; set; }
        [DataMember]
        public IDictionary<string, LineSegment> LineSegments { get; set; }
        public bool Run(IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage)
        {
            Buffer(items);

            bool foundItem = false;

            IList<IFramedItem> itemsToPost = new List<IFramedItem>();

            for (int i = 0; i < items.Count; i++)
            {
                for (int j = 0; j < items[i].ItemIDs.Count; j++)
                {
                    var id = items[i].ItemIDs[j];
                    if (id.SourceObject != previousStage)
                    {
                        continue;
                    }
                    if (id.SourceObject == previousStage && id.FurtherAnalysisTriggered)
                    {
                        Debug.Assert(id is ILineTriggeredItemID);

                        var ltid = (ILineTriggeredItemID)id;

                        IList<IFramedItem> intersections = FindIntersectingObjects(ltid, frame);

                        IFramedItem bestMatch = null;
                        float bestScore = 0.0f;
                        float bestOverlap = 0.0f;
                        float bestConfidence = 0.0f;
                        for (int k = 0; k < intersections.Count; k++)
                        {
                            var hci = intersections[k].HighestConfidenceIndex;
                            var hcid = intersections[k].ItemIDs[hci];
                            float confidence = (float)hcid.Confidence;
                            float value = Utils.LineOverlapFilter.GetOverlapRatio(ltid.TriggerSegment, intersections[k].MeanBounds);
                            float score = value * confidence;
                            if (score > bestScore)
                            {
                                bestMatch = intersections[k];
                                bestScore = score;
                                bestOverlap = value;
                                bestConfidence = confidence;
                            }
                        }

                        if (bestMatch == null)
                        {
                            continue;
                        }

                        int index = bestMatch.HighestConfidenceIndex;
                        IItemID bestID = bestMatch.ItemIDs[index];

                        LineTriggeredItemID myid = new(bestID)
                        {
                            SourceObject = this,
                            FurtherAnalysisTriggered = true,
                            TriggerLineID = ltid.TriggerLineID,
                            TriggerLine = ltid.TriggerLine,
                            TriggerSegment = ltid.TriggerSegment
                        };
                        bestMatch.ItemIDs.Add(myid);
                        if(!items.Contains(bestMatch))
                        {
                            itemsToPost.Add(bestMatch);
                        }
                        foundItem = true;
                    }
                }
            }

            for (int i = 0; i < itemsToPost.Count; i++)
            {
                items.Add(itemsToPost[i]);
            }
            return foundItem;
        }

        private void Buffer(IList<IFramedItem> items)
        {
            _itemBuffer.Add(items);
            if (_itemBuffer.Count > BufferSize)
            {
                _itemBuffer.RemoveRange(0, _itemBuffer.Count - BufferSize);
            }
        }

        private IList<IFramedItem> FindIntersectingObjects(ILineTriggeredItemID ltid, IFrame frame)
        {
            LineSegment segment = ltid.TriggerSegment;
            List<IFramedItem> results = new List<IFramedItem>();

            for (int i = _itemBuffer.Count - 1; i >= 0; --i)
            {
                var itemSet = _itemBuffer[i];
                var overlaps = Utils.LineOverlapFilter.GetItemOverlap(itemSet, (IFramedItem fi) => new RectangleF(fi.MeanBounds.X, fi.MeanBounds.Y, fi.MeanBounds.Width, fi.MeanBounds.Height), segment);
                IFramedItem bestMatch = null;
                float bestScore = 0.0f;
                float bestOverlap = 0.0f;
                float bestConfidence = 0.0f;
                foreach (var entry in overlaps)
                {
                    if(entry.Value < 0.01)
                    {
                        continue;
                    }
                    var hci = entry.Key.HighestConfidenceIndex;
                    var hcid = entry.Key.ItemIDs[hci];
                    float confidence = (float)hcid.Confidence;
                    float score = entry.Value * confidence;
                    if(score > bestScore)
                    {
                        bestMatch = entry.Key;
                        bestScore = score;
                        bestOverlap = entry.Value;
                        bestConfidence = confidence;
                    }
                }

                if (bestMatch != null && bestScore > 0.0f)
                {
                    results.Add(bestMatch);
                }
            }
            return results;
        }
    }
}
