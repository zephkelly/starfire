using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for InitWaypointStackAction.
    /// Initializes the waypoint stack from waypoints (supports both static and dynamic).
    /// </summary>
    [Serializable]
    public class InitWaypointStackParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the List of Vector2 waypoints (static).
        /// </summary>
        public string waypointsKey = "waypoint_list";

        /// <summary>
        /// Blackboard key containing the List of Transform waypoints (dynamic).
        /// If present, takes priority over waypointsKey for live tracking.
        /// </summary>
        public string transformsKey = "waypoint_transforms";

        /// <summary>
        /// Blackboard key to store the WaypointStackState.
        /// </summary>
        public string stackKey = "waypoint_stack";

        /// <summary>
        /// Traversal mode for the base patrol sequence.
        /// </summary>
        public WaypointTraversalMode mode = WaypointTraversalMode.Loop;

        /// <summary>
        /// Maximum nesting depth for the stack.
        /// </summary>
        public int maxDepth = 5;
    }
}
