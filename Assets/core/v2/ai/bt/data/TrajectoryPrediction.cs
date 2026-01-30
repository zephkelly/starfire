using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Result of trajectory prediction calculations.
    /// Stored in blackboard for use by multiple BT nodes.
    /// </summary>
    [Serializable]
    public struct TrajectoryPrediction
    {
        /// <summary>
        /// Closest point on current trajectory to target.
        /// </summary>
        public Vector2 ClosestApproachPoint;

        /// <summary>
        /// Distance from closest approach point to target.
        /// </summary>
        public float MissDistance;

        /// <summary>
        /// Time until closest approach (seconds).
        /// </summary>
        public float TimeToClosestApproach;

        /// <summary>
        /// True if ship will pass by target without reaching it.
        /// </summary>
        public bool WillMiss;

        /// <summary>
        /// Angle between velocity and target direction (0-180 degrees).
        /// 0 = heading directly at target, 90 = perpendicular, 180 = moving away.
        /// </summary>
        public float ApproachAngle;

        /// <summary>
        /// Component of velocity perpendicular to target direction.
        /// High lateral speed indicates orbiting risk.
        /// </summary>
        public float LateralSpeed;

        /// <summary>
        /// Component of velocity toward/away from target.
        /// Positive = approaching, negative = receding.
        /// </summary>
        public float RadialSpeed;

        /// <summary>
        /// The radial velocity vector (toward/away from target).
        /// </summary>
        public Vector2 RadialVelocity;

        /// <summary>
        /// The lateral velocity vector (perpendicular to target direction).
        /// </summary>
        public Vector2 LateralVelocity;

        /// <summary>
        /// True if we've already passed the target (moving away).
        /// </summary>
        public bool HasOvershot;

        /// <summary>
        /// Current distance to target.
        /// </summary>
        public float DistanceToTarget;

        /// <summary>
        /// Current speed.
        /// </summary>
        public float CurrentSpeed;
    }
}
