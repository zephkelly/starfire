using System;

namespace StarfireV2
{
    /// <summary>
    /// Comparison types for stack depth checks.
    /// </summary>
    public enum ComparisonType
    {
        Equal,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

    /// <summary>
    /// Parameters for IsStackDepthCondition.
    /// Checks if the waypoint stack is at a certain depth.
    /// </summary>
    [Serializable]
    public class IsStackDepthParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the WaypointStackState.
        /// </summary>
        public string stackKey = "waypoint_stack";

        /// <summary>
        /// The depth value to compare against.
        /// </summary>
        public int depth = 1;

        /// <summary>
        /// How to compare the stack depth to the target depth.
        /// </summary>
        public ComparisonType comparison = ComparisonType.GreaterThan;
    }
}
