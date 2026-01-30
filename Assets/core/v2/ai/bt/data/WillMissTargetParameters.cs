using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for WillMissTargetCondition.
    /// Checks if current trajectory will miss the target.
    /// </summary>
    [Serializable]
    public class WillMissTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for trajectory prediction data (TrajectoryPrediction).
        /// </summary>
        public string predictionKey = "trajectory_prediction";

        /// <summary>
        /// Distance threshold above which trajectory is considered a "miss".
        /// </summary>
        public float missThreshold = 2.0f;
    }
}
