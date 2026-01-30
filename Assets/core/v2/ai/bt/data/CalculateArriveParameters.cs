using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for CalculateArriveAction.
    /// Seeks target with deceleration to stop at destination.
    /// Uses physics-based stopping distance calculation: v² / (2a)
    /// </summary>
    [Serializable]
    public class CalculateArriveParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for target position (Vector2).
        /// </summary>
        public string targetKey = "steering_target";

        /// <summary>
        /// Blackboard key to write the calculated steering force (Vector2).
        /// </summary>
        public string outputKey = "steering_force";

        /// <summary>
        /// Distance threshold for considering arrival complete.
        /// </summary>
        public float arrivalThreshold = 1.0f;
    }
}
