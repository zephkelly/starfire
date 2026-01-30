using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for GeneratePatrolWaypointsAction.
    /// Generates waypoints around a center position if none exist.
    /// </summary>
    [Serializable]
    public class GeneratePatrolWaypointsParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to write the waypoint list.
        /// </summary>
        public string outputKey = "waypoint_list";

        /// <summary>
        /// Blackboard key for the patrol center position.
        /// If not found, uses current position.
        /// </summary>
        public string centerKey = "patrol_center";

        /// <summary>
        /// Blackboard key for the patrol radius.
        /// If not found, uses defaultRadius.
        /// </summary>
        public string radiusKey = "patrol_radius";

        /// <summary>
        /// Default radius if not found in blackboard.
        /// </summary>
        public float defaultRadius = 50f;

        /// <summary>
        /// Number of waypoints to generate.
        /// </summary>
        public int waypointCount = 4;

        /// <summary>
        /// If true, skips generation if waypoints already exist.
        /// </summary>
        public bool skipIfExists = true;
    }
}
