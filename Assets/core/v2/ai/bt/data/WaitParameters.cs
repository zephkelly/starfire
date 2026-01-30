using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for WaitAction.
    /// Pauses execution for a specified duration.
    /// </summary>
    [Serializable]
    public class WaitParameters : IBTNodeParameters
    {
        /// <summary>
        /// How long to wait in seconds.
        /// </summary>
        public float duration = 1.0f;

        /// <summary>
        /// Random variance added to duration. Actual wait = duration + Random(0, randomDelay).
        /// Set to 0 for exact timing.
        /// </summary>
        public float randomDelay = 0f;

        /// <summary>
        /// Blackboard key to track elapsed wait time.
        /// </summary>
        public string elapsedKey = "wait_elapsed";
    }
}
