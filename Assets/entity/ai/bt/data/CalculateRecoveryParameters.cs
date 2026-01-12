using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for CalculateRecoveryAction.
    /// Handles recovery when ship has overshot the target.
    /// </summary>
    [Serializable]
    public class CalculateRecoveryParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for target position (Vector2).
        /// </summary>
        public string targetKey = "steering_target";

        /// <summary>
        /// Blackboard key for trajectory prediction data (TrajectoryPrediction).
        /// </summary>
        public string predictionKey = "trajectory_prediction";

        /// <summary>
        /// Blackboard key to write the calculated steering force (Vector2).
        /// </summary>
        public string outputKey = "steering_force";

        /// <summary>
        /// Speed threshold for recovery mode selection.
        /// Above this: gentle loop back. Below this: aggressive correction.
        /// </summary>
        public float speedThreshold = 5.0f;

        /// <summary>
        /// Blend factor for turn vs brake when looping back (0-1).
        /// Higher = more turning, lower = more braking.
        /// </summary>
        public float turnBrakeFactor = 0.7f;
    }
}
