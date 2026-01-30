using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for BTGuardedRepeater decorator node.
    /// A guarded repeater has two children: a guard condition and a body.
    /// The guard is checked each tick before executing the body.
    /// If the guard succeeds, the loop is interrupted.
    /// </summary>
    [Serializable]
    public class GuardedRepeaterParameters : IBTNodeParameters
    {
        /// <summary>
        /// Number of times to repeat. -1 means repeat until guard triggers.
        /// </summary>
        public int repeatCount = -1;
    }
}
