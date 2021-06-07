// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessingPipeline
{
    /// <summary>
    ///   A falling edge trigger that fires when a value drops below some threshold.
    /// </summary>
    public class FallingEdgeTrigger : IEdgeTrigger
    {
        /// <inheritdoc/>
        public IList<double> ValueHistory { get; set; }

        /// <inheritdoc/>
        /// </summary>
        public double TriggerThreshold { get; set; }

        /// <inheritdoc/>
        public double Hysteresis { get; set; }

        /// <inheritdoc/>
        public int MinimumEntriesForTransition { get; set; }

        /// <inheritdoc/>
        public bool CurrentState { get; protected set; }

        /// <inheritdoc/>
        public bool NotifyNextValue(double value)
        {
            ValueHistory.Add(value);

            if (CurrentState && value < TriggerThreshold - Hysteresis)
            {
                int frameCount = 1;
                for (int i = ValueHistory.Count - 2; i >= 0; i++)
                {
                    if (ValueHistory[i] < TriggerThreshold - Hysteresis)
                    {
                        ++frameCount;
                        if (frameCount >= MinimumEntriesForTransition)
                        {
                            CurrentState = false;
                            return true;
                        }
                    }
                }
            }
            else if (!CurrentState && value > TriggerThreshold + Hysteresis)
            {
                int frameCount = 1;
                for (int i = ValueHistory.Count - 2; i >= 0; i++)
                {
                    if (ValueHistory[i] > TriggerThreshold + Hysteresis)
                    {
                        ++frameCount;
                        if (frameCount >= MinimumEntriesForTransition)
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
