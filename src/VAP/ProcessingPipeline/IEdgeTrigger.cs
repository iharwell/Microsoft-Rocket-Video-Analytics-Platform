// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessingPipeline
{
    /// <summary>
    ///   Interface for triggers that fire upon some value crossing a threshold.
    /// </summary>
    public interface IEdgeTrigger
    {
        /// <summary>
        ///   The record of previous values.
        /// </summary>
        IList<double> ValueHistory { get; set; }

        /// <summary>
        ///   The threshold that determines when this trigger fires. Note that the actual firing
        ///   value is also affected by <see cref="Hysteresis" />.
        /// </summary>
        double TriggerThreshold { get; set; }

        /// <summary>
        ///   The amount of hysteresis used by the trigger. When the current state is low, this is
        ///   added to <see cref="TriggerThreshold" />. When the state is high, it is subtracted.
        /// </summary>
        double Hysteresis { get; set; }

        /// <summary>
        ///   The number of consecutive entries that are needed before this trigger changes state.
        /// </summary>
        int MinimumEntriesForTransition { get; set; }

        /// <summary>
        ///   Notifies the trigger of the next value to take into account.
        /// </summary>
        /// <param name="value">
        ///   The value observed by the trigger to determine when to fire.
        /// </param>
        /// <returns>
        ///   Returns <see langword="true"/> to indicate that the trigger has fired; otherwise <see langword="false"/>.
        /// </returns>
        bool NotifyNextValue(double value);

        /// <summary>
        ///   The current state of this trigger.
        /// </summary>
        bool CurrentState { get; }
    }
}
