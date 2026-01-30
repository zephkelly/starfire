using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for IsInRangeCondition.
    /// Checks if entity is within min/max range of target.
    /// </summary>
    [Serializable]
    public class IsInRangeParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for target position (Vector2).
        /// </summary>
        public string targetKey = "steering_target";

        /// <summary>
        /// Minimum range (must be at least this far).
        /// </summary>
        public float minRange = 0f;

        /// <summary>
        /// Maximum range (must be at most this far).
        /// </summary>
        public float maxRange = 50f;
    }
}
