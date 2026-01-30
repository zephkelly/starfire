using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for CalculateTrajectoryPredictionAction.
    /// Calculates trajectory prediction data and stores it in blackboard.
    /// </summary>
    [Serializable]
    public class CalculateTrajectoryPredictionParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for target position (Vector2).
        /// </summary>
        public string targetKey = "steering_target";

        /// <summary>
        /// Blackboard key to write the prediction result (TrajectoryPrediction).
        /// </summary>
        public string predictionKey = "trajectory_prediction";

        /// <summary>
        /// Distance threshold for considering a trajectory as "will miss".
        /// </summary>
        public float missThreshold = 2.0f;
    }
}
