using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for AdvanceWaypointAction.
    /// </summary>
    [Serializable]
    public class AdvanceWaypointParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to read waypoints list from (for count/wrap calculation).
        /// </summary>
        public string waypointsKey = "patrol_waypoints";

        /// <summary>
        /// Blackboard key to store/read the current waypoint index.
        /// </summary>
        public string indexKey = "waypoint_index";

        /// <summary>
        /// How to traverse waypoints when reaching the end.
        /// Loop: wraps back to start. PingPong: reverses direction.
        /// </summary>
        public WaypointTraversalMode traversalMode = WaypointTraversalMode.Loop;

        /// <summary>
        /// Blackboard key to store traversal direction for PingPong mode (1 or -1).
        /// </summary>
        public string directionKey = "waypoint_direction";
    }
}
