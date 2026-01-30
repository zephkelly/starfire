using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for HasOvershotCondition.
    /// Checks if ship has passed the target and is moving away.
    /// </summary>
    [Serializable]
    public class HasOvershotParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for trajectory prediction data (TrajectoryPrediction).
        /// </summary>
        public string predictionKey = "trajectory_prediction";

        /// <summary>
        /// Minimum distance from target to consider it an overshoot.
        /// Prevents false positives when very close to target.
        /// </summary>
        public float minDistance = 0.5f;
    }
}
