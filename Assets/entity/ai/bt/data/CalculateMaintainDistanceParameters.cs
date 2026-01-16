using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for CalculateMaintainDistanceAction.
    /// Maintains position inside the sensor's Silhouette detection range.
    /// </summary>
    [Serializable]
    public class CalculateMaintainDistanceParameters : IBTNodeParameters
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
        /// How far inside the Silhouette detection range to hold position.
        /// The actual distance from target = SilhouetteRange - bufferDistance.
        /// </summary>
        public float bufferDistance = 15f;

        /// <summary>
        /// How aggressively to correct distance errors.
        /// Higher values = faster correction but may overshoot.
        /// </summary>
        public float correctionFactor = 2f;

        /// <summary>
        /// Multiplier for lateral velocity damping.
        /// Higher values = faster lateral correction (less orbiting).
        /// </summary>
        public float lateralBrakingFactor = 1.5f;
    }
}
