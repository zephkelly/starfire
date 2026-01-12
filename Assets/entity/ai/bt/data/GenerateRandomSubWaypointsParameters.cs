using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for GenerateRandomSubWaypointsAction.
    /// Generates random exploration waypoints around a center point.
    /// </summary>
    [Serializable]
    public class GenerateRandomSubWaypointsParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the center position. If empty, uses ship's current position.
        /// </summary>
        public string centerKey = "steering_target";

        /// <summary>
        /// Blackboard key to store the generated waypoints (List of Vector2).
        /// </summary>
        public string outputKey = "generated_subwaypoints";

        /// <summary>
        /// Number of sub-waypoints to generate.
        /// </summary>
        public int count = 3;

        /// <summary>
        /// Minimum distance from center for generated waypoints.
        /// </summary>
        public float minRadius = 5f;

        /// <summary>
        /// Maximum distance from center for generated waypoints.
        /// </summary>
        public float maxRadius = 15f;

        /// <summary>
        /// If true, excludes the center point from generated waypoints.
        /// If false, may include the center as the final return point.
        /// </summary>
        public bool avoidCenter = true;
    }
}
