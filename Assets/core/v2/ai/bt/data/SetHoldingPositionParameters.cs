using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for SetHoldingPositionAction.
    /// Calculates the holding position based on sensor's Silhouette range.
    /// </summary>
    [Serializable]
    public class SetHoldingPositionParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the target entity to maintain distance from.
        /// </summary>
        public string targetEntityKey = "monitored_target";

        /// <summary>
        /// Blackboard key to write the calculated holding position (Vector2).
        /// </summary>
        public string outputKey = "steering_target";

        /// <summary>
        /// How far inside the Silhouette detection range to hold position.
        /// The holding position = SilhouetteRange - bufferDistance from target.
        /// </summary>
        public float bufferDistance = 15f;
    }
}
