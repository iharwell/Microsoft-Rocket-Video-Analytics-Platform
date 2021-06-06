using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessingPipeline
{
    public class FallingEdgeTrigger : IEdgeTrigger
    {
        public IList<double> ValueHistory { get; set; }
        public double TriggerThreshold { get; set; }
        public double Hysteresis { get; set; }
        public int MinimumEntriesForTransition { get; set; }
        public bool CurrentState { get; protected set; }
        public bool NotifyNextValue( double value )
        {
            ValueHistory.Add( value );

            if( CurrentState && value < TriggerThreshold - Hysteresis )
            {
                int frameCount = 1;
                for ( int i = ValueHistory.Count - 2; i >= 0; i++ )
                {
                    if ( ValueHistory[i] < TriggerThreshold - Hysteresis )
                    {
                        ++frameCount;
                        if ( frameCount >= MinimumEntriesForTransition )
                        {
                            CurrentState = false;
                            return true;
                        }
                    }
                }
            }
            else if ( !CurrentState && value > TriggerThreshold + Hysteresis )
            {
                int frameCount = 1;
                for ( int i = ValueHistory.Count - 2; i >= 0; i++ )
                {
                    if ( ValueHistory[i] > TriggerThreshold + Hysteresis )
                    {
                        ++frameCount;
                        if ( frameCount >= MinimumEntriesForTransition )
                        {
                            CurrentState = true;
                            return false;
                        }
                    }
                }
            }
            return false;
        }
    }
}
