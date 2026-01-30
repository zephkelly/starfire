using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for SetTargetFromWaypointsAction.
    /// Copies the current waypoint position to the target key.
    /// </summary>
    [Serializable]
    public class SetTargetFromWaypointsParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the List of Vector2 waypoints.
        /// </summary>
        public string waypointsKey = "waypoint_list";

        /// <summary>
        /// Blackboard key containing the current waypoint index (int).
        /// </summary>
        public string indexKey = "waypoint_index";

        /// <summary>
        /// Blackboard key to write the current waypoint position.
        /// </summary>
        public string targetKey = "steering_target";

        /// <summary>
        /// Blackboard key for the WaypointStackState (stack-based waypoints).
        /// If present, stack-based waypoints take priority over legacy list.
        /// </summary>
        public string stackKey = "waypoint_stack";
    }
}
