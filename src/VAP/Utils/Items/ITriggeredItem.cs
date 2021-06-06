// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Items
{
    public interface ITriggeredItem : IItemID
    {
        bool FurtherAnalysisTriggered { get; set; }

    }
}
