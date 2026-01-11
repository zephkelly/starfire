using System;
using Starfire.Entity.AI.Steering;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for CalculateSteeringAction.
    /// </summary>
    [Serializable]
    public class CalculateSteeringParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the target position to steer toward (Vector2).
        /// </summary>
        public string targetKey = "move_target";

        /// <summary>
        /// Blackboard key to write the calculated steering direction (Vector2 normalized).
        /// </summary>
        public string directionOutputKey = "steering_direction";

        /// <summary>
        /// Blackboard key to write the throttle value (float 0-1).
        /// </summary>
        public string throttleOutputKey = "steering_throttle";

        /// <summary>
        /// Blackboard key to write whether arrival is complete (bool).
        /// </summary>
        public string arrivedOutputKey = "steering_arrived";

        /// <summary>
        /// Blackboard key to write the full steering force vector (Vector2).
        /// </summary>
        public string steeringForceOutputKey = "steering_force";

        /// <summary>
        /// Distance at which the agent is considered to have arrived.
        /// </summary>
        public float arrivalThreshold = 1f;

        /// <summary>
        /// Velocity magnitude threshold for arrival detection.
        /// Ship must be slower than this to be considered arrived.
        /// </summary>
        public float velocityThreshold = 0.5f;

        /// <summary>
        /// Multiplier for the slowing radius (slowingRadius = arrivalThreshold * this value).
        /// Only used by ControlledApproachStrategy.
        /// </summary>
        public float slowingRadiusMultiplier = 5f;

        /// <summary>
        /// Multiplier applied to arrivalThreshold for the final arrival check.
        /// Default 2.0 means arrival at 2x the base threshold distance.
        /// </summary>
        public float arrivalDistanceMultiplier = 2f;

        /// <summary>
        /// Multiplier applied to velocityThreshold for the final arrival check.
        /// Default 3.0 means arrival when speed is less than 3x base velocity threshold.
        /// </summary>
        public float arrivalVelocityMultiplier = 3f;

        /// <summary>
        /// Default approach strategy to use.
        /// </summary>
        public ApproachStrategyType defaultStrategy = ApproachStrategyType.Controlled;

        /// <summary>
        /// Blackboard key for approach strategy override (ApproachStrategyType).
        /// If set in blackboard, overrides defaultStrategy.
        /// </summary>
        public string strategyKey = "approach_strategy";

        /// <summary>
        /// Blackboard key for the next waypoint position (Vector2).
        /// Used by flyby strategy to calculate smooth curves.
        /// </summary>
        public string nextWaypointKey = "next_waypoint";
    }
}
