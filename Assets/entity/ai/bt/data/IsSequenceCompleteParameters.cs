using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for IsSequenceCompleteCondition.
    /// Checks if the current waypoint sequence is complete.
    /// </summary>
    [Serializable]
    public class IsSequenceCompleteParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the WaypointStackState.
        /// </summary>
        public string stackKey = "waypoint_stack";
    }
}
