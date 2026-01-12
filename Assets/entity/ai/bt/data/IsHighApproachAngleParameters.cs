using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for IsHighApproachAngleCondition.
    /// Returns true when the approach angle to target exceeds threshold.
    /// Used to detect when ship is facing significantly away from its target.
    /// </summary>
    [Serializable]
    public class IsHighApproachAngleParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for trajectory prediction data (TrajectoryPrediction).
        /// </summary>
        public string predictionKey = "trajectory_prediction";

        /// <summary>
        /// Angle threshold in degrees. Returns true if approach angle exceeds this.
        /// Default 90° means the condition triggers when ship is facing more than
        /// perpendicular to the target direction.
        /// </summary>
        public float angleThreshold = 90f;
    }
}
