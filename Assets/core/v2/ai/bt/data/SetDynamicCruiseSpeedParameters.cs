using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for SetDynamicCruiseSpeedAction.
    /// Calculates cruise speed dynamically based on waypoint distances.
    /// Speed is interpolated between min/max percentages based on effective distance.
    /// </summary>
    [Serializable]
    public class SetDynamicCruiseSpeedParameters : IBTNodeParameters
    {
        /// <summary>
        /// Minimum cruise speed as percentage of ship's max speed (0.0-1.0).
        /// Used when at or below minDistance.
        /// </summary>
        public float minSpeedPercent = 0.3f;

        /// <summary>
        /// Maximum cruise speed as percentage of ship's max speed (0.0-1.0).
        /// Used when at or above maxDistance.
        /// </summary>
        public float maxSpeedPercent = 0.8f;

        /// <summary>
        /// Distance threshold for minimum speed (world units).
        /// </summary>
        public float minDistance = 10f;

        /// <summary>
        /// Distance threshold for maximum speed (world units).
        /// </summary>
        public float maxDistance = 100f;

        /// <summary>
        /// How much the next waypoint segment influences speed (0.0-1.0).
        /// 0 = only consider current waypoint distance.
        /// 1 = fully add next segment length to effective distance.
        /// </summary>
        public float segmentInfluence = 0.5f;

        /// <summary>
        /// Whether to also scale acceleration proportionally to speed.
        /// </summary>
        public bool scaleAcceleration = false;

        /// <summary>
        /// Minimum acceleration as percentage of ship's max acceleration.
        /// Only used if scaleAcceleration is true.
        /// </summary>
        public float minAccelPercent = 0.5f;

        /// <summary>
        /// Maximum acceleration as percentage of ship's max acceleration.
        /// Only used if scaleAcceleration is true.
        /// </summary>
        public float maxAccelPercent = 1.0f;

        /// <summary>
        /// Blackboard key for the current steering target (Vector2).
        /// </summary>
        public string targetKey = "steering_target";

        /// <summary>
        /// Blackboard key for the WaypointStackState.
        /// Used to access next waypoint for segment length calculation.
        /// </summary>
        public string stackKey = "waypoint_stack";

        /// <summary>
        /// Blackboard key to write the calculated cruise speed.
        /// </summary>
        public string speedKey = "cruise_speed";

        /// <summary>
        /// Blackboard key to write the cruise acceleration.
        /// </summary>
        public string accelKey = "cruise_acceleration";
    }
}
