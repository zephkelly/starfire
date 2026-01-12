using System;

namespace Starfire.Entity.AI.BT
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
        /// Blackboard key to track elapsed wait time.
        /// </summary>
        public string elapsedKey = "wait_elapsed";
    }
}
