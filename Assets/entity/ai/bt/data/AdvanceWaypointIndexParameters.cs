using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for AdvanceWaypointIndexAction.
    /// Increments the waypoint index with Loop or PingPong modes.
    /// </summary>
    [Serializable]
    public class AdvanceWaypointIndexParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the List of Vector2 waypoints (static).
        /// </summary>
        public string waypointsKey = "waypoint_list";

        /// <summary>
        /// Blackboard key containing the List of Transform waypoints (dynamic).
        /// Used for count in legacy mode if waypointsKey not found.
        /// </summary>
        public string transformsKey = "waypoint_transforms";

        /// <summary>
        /// Blackboard key containing the current waypoint index (int).
        /// </summary>
        public string indexKey = "waypoint_index";

        /// <summary>
        /// How to traverse the waypoints.
        /// </summary>
        public WaypointTraversalMode mode = WaypointTraversalMode.Loop;

        /// <summary>
        /// Blackboard key for ping-pong direction (int: 1 or -1).
        /// Only used in PingPong mode.
        /// </summary>
        public string directionKey = "waypoint_direction";

        /// <summary>
        /// Blackboard key for the WaypointStackState (stack-based waypoints).
        /// If present, stack-based waypoints take priority over legacy list.
        /// </summary>
        public string stackKey = "waypoint_stack";
    }

    /// <summary>
    /// How to traverse waypoints when reaching the end.
    /// </summary>
    public enum WaypointTraversalMode
    {
        /// <summary>
        /// Loop back to start after reaching end.
        /// </summary>
        Loop,

        /// <summary>
        /// Reverse direction at each end.
        /// </summary>
        PingPong,

        /// <summary>
        /// Stop at the last waypoint.
        /// </summary>
        Once
    }
}
