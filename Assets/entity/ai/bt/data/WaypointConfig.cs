using System;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Configuration for a single waypoint's behavior.
    /// Allows per-waypoint control of stop vs fly-through behavior.
    /// </summary>
    [Serializable]
    public class WaypointConfig
    {
        /// <summary>
        /// The position of this waypoint.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// True = must stop at waypoint. False = fly-through (pass within radius).
        /// </summary>
        public bool RequiresStop = true;

        /// <summary>
        /// For fly-through waypoints: radius within which waypoint is considered reached.
        /// </summary>
        public float FlyThroughRadius = 3f;

        /// <summary>
        /// Optional speed limit when approaching this waypoint.
        /// -1 = no limit.
        /// </summary>
        public float ApproachSpeedLimit = -1f;

        public WaypointConfig() { }

        public WaypointConfig(Vector2 position, bool requiresStop = true, float flyThroughRadius = 3f)
        {
            Position = position;
            RequiresStop = requiresStop;
            FlyThroughRadius = flyThroughRadius;
        }
    }
}
