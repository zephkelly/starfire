using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for CalculateBrakeAndTurnAction.
    /// Used when ship is facing significantly away from target and needs to
    /// brake while rotating toward it, rather than arcing.
    /// </summary>
    [Serializable]
    public class CalculateBrakeAndTurnParameters : IBTNodeParameters
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
        /// Proportion of max acceleration used for braking (0-1).
        /// Higher values = more braking, less correction toward target.
        /// Default 0.8 means 80% braking, 20% correction.
        /// </summary>
        public float brakeFactor = 0.8f;

        /// <summary>
        /// Speed below which braking is complete and action returns Success.
        /// Once speed drops below this, normal steering can take over.
        /// </summary>
        public float minSpeedThreshold = 2f;
    }
}
