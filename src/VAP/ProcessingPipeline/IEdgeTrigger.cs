using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessingPipeline
{
    public interface IEdgeTrigger
    {
        IList<double> ValueHistory { get; set; }

        double TriggerThreshold { get; set; }

        double Hysteresis { get; set; }

        int MinimumEntriesForTransition { get; set; }

        bool NotifyNextValue( double value );

        bool CurrentState { get; }
    }
}
