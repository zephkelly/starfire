using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for SetTargetFromWaypointsDynamicAction.
    /// Copies the current waypoint position to the target key using live Transform references.
    /// </summary>
    [Serializable]
    public class SetTargetFromWaypointsDynamicParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the List of Transform waypoints.
        /// </summary>
        public string waypointsKey = "waypoint_transforms";

        /// <summary>
        /// Blackboard key containing the current waypoint index (int).
        /// </summary>
        public string indexKey = "waypoint_index";

        /// <summary>
        /// Blackboard key to write the current waypoint position.
        /// </summary>
        public string targetKey = "steering_target";
    }
}
