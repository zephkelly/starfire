using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for CalculateSmartArriveAction.
    /// Angle-aware, prediction-based arrival steering with compound braking.
    /// </summary>
    [Serializable]
    public class CalculateSmartArriveParameters : IBTNodeParameters
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
        /// Distance threshold for considering arrival complete.
        /// </summary>
        public float arrivalThreshold = 1.0f;

        /// <summary>
        /// How much poor approach angles increase braking distance.
        /// Higher values = earlier braking for angled approaches.
        /// </summary>
        public float angleStiffness = 1.5f;

        /// <summary>
        /// Aggression for lateral velocity cancellation (perpendicular drift).
        /// Higher values = faster lateral correction.
        /// </summary>
        public float lateralBrakingFactor = 2.0f;

        /// <summary>
        /// Distance at which turn-then-burn behavior engages.
        /// Within this range, thrust is reduced when not facing target.
        /// Set to 0 to disable.
        /// </summary>
        public float closeRangeThreshold = 10f;

        /// <summary>
        /// Alignment angle in degrees. Full thrust is applied when
        /// approach angle is within this value. Default 30 degrees.
        /// </summary>
        public float alignmentAngle = 30f;
    }
}
