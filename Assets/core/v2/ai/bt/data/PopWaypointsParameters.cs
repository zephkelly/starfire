using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for PopWaypointsAction.
    /// Pops the current waypoint sequence and returns to the previous one.
    /// </summary>
    [Serializable]
    public class PopWaypointsParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the WaypointStackState.
        /// </summary>
        public string stackKey = "waypoint_stack";
    }
}
