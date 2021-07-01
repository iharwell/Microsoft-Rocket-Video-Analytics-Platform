// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.Items;

namespace Utils
{
    public class SimpleIndexChooser : IIndexChooser
    {
        public SimpleIndexChooser(int stride)
        {
            Stride = stride;
        }

        public int Stride { get; set; }

        public int BufferDepth { get; set; }

        public int ChooseNextIndex(IDictionary<int, IEnumerable<IItemID>> priorIDs, IList<(IFramedItem, ILineTriggeredItemID)> triggerIDs, int prevIndex, int latestFrame)
        {
            for (int i = 1; i < Stride; i++)
            {
                if(i == 0)
                {
                    continue;
                }
                if (priorIDs.ContainsKey(prevIndex - i))
                {
                    return prevIndex - i;
                }
            }
            return prevIndex - Stride;
        }
        public int FirstIndex(IDictionary<int, IEnumerable<IItemID>> priorIDs, IList<(IFramedItem, ILineTriggeredItemID)> triggerIDs, int latestFrame)
        {
            for (int i = 0; i < Stride; i++)
            {
                if (priorIDs.ContainsKey(latestFrame - i))
                {
                    return latestFrame - i;
                }
            }
            return latestFrame - 1;
        }
    }
}
