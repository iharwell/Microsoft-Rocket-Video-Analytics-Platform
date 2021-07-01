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
    public interface IIndexChooser
    {
        int BufferDepth { get; set; }

        int ChooseNextIndex(IDictionary<int, IEnumerable<IItemID>> priorIDs, IList<(IFramedItem, ILineTriggeredItemID)> triggerIDs, int prevFrame, int latestFrame);
        int FirstIndex(IDictionary<int, IEnumerable<IItemID>> priorIDs, IList<(IFramedItem, ILineTriggeredItemID)> triggerIDs, int latestFrame);
    }
}
