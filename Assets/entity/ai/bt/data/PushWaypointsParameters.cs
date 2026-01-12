using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for PushWaypointsAction.
    /// Pushes a new waypoint sequence onto the stack.
    /// </summary>
    [Serializable]
    public class PushWaypointsParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the WaypointStackState.
        /// </summary>
        public string stackKey = "waypoint_stack";

        /// <summary>
        /// Blackboard key containing the List of Vector2 waypoints to push.
        /// </summary>
        public string waypointsKey = "generated_subwaypoints";

        /// <summary>
        /// Traversal mode for the new sequence (typically Once for sub-waypoints).
        /// </summary>
        public WaypointTraversalMode mode = WaypointTraversalMode.Once;

        /// <summary>
        /// Debug label for the new sequence.
        /// </summary>
        public string label = "exploration";
    }
}
