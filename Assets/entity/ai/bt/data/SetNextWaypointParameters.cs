using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for SetNextWaypointAction.
    /// </summary>
    [Serializable]
    public class SetNextWaypointParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to read waypoints list from.
        /// </summary>
        public string waypointsKey = "patrol_waypoints";

        /// <summary>
        /// Blackboard key to write the current waypoint target to.
        /// </summary>
        public string targetKey = "move_target";

        /// <summary>
        /// Blackboard key to store/read the current waypoint index.
        /// </summary>
        public string indexKey = "waypoint_index";
    }
}
