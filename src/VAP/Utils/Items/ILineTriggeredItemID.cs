using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Items
{
    public interface ILineTriggeredItemID : IItemID
    {
        string TriggerLine { get; set; }
        int TriggerLineID { get; set; }
    }
}
