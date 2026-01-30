using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for CalculateFlyThroughAction.
    /// Calculates steering for fly-through waypoints (no stop required).
    /// </summary>
    [Serializable]
    public class CalculateFlyThroughParameters : IBTNodeParameters
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
        /// Radius within which waypoint is considered passed.
        /// </summary>
        public float passRadius = 3.0f;
    }
}
