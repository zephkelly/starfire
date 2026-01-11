using UnityEngine;

namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Context for approach strategy calculations.
    /// Contains physics data, waypoint information, and optional tactical data.
    /// </summary>
    public class ApproachContext
    {
        // Core physics data
        public Vector2 Position;
        public Vector2 Velocity;
        public float MaxSpeed;
        public float MaxAcceleration;

        // Waypoint data
        public Vector2 TargetWaypoint;
        public Vector2? NextWaypoint;       // null if this is the last waypoint
        public float ArrivalThreshold;
        public float VelocityThreshold;

        // Arrival multipliers (configurable per-node)
        public float ArrivalDistanceMultiplier;
        public float ArrivalVelocityMultiplier;

        /// <summary>
        /// Effective arrival distance (base threshold * multiplier).
        /// </summary>
        public float EffectiveArrivalDistance => ArrivalThreshold * ArrivalDistanceMultiplier;

        /// <summary>
        /// Effective velocity threshold for arrival (base threshold * multiplier).
        /// </summary>
        public float EffectiveVelocityThreshold => VelocityThreshold * ArrivalVelocityMultiplier;

        // Optional tactical data (for future combat-aware strategies)
        public float? Health;               // 0-1 normalized, null if not applicable
        public Vector2[] NearbyEnemies;     // positions of threats, null if not applicable

        /// <summary>
        /// Creates an ApproachContext from a SteeringContext with waypoint data.
        /// </summary>
        public static ApproachContext FromSteering(
            SteeringContext steering,
            Vector2 target,
            Vector2? nextWaypoint = null,
            float arrivalThreshold = 1f,
            float velocityThreshold = 0.5f,
            float arrivalDistanceMultiplier = 2f,
            float arrivalVelocityMultiplier = 3f)
        {
            return new ApproachContext
            {
                Position = steering.Position,
                Velocity = steering.Velocity,
                MaxSpeed = steering.MaxSpeed,
                MaxAcceleration = steering.MaxAcceleration,
                TargetWaypoint = target,
                NextWaypoint = nextWaypoint,
                ArrivalThreshold = arrivalThreshold,
                VelocityThreshold = velocityThreshold,
                ArrivalDistanceMultiplier = arrivalDistanceMultiplier,
                ArrivalVelocityMultiplier = arrivalVelocityMultiplier
            };
        }

        /// <summary>
        /// Distance to the target waypoint.
        /// </summary>
        public float DistanceToTarget => Vector2.Distance(Position, TargetWaypoint);

        /// <summary>
        /// Direction to the target waypoint (normalized).
        /// </summary>
        public Vector2 DirectionToTarget
        {
            get
            {
                var toTarget = TargetWaypoint - Position;
                return toTarget.magnitude > 0.001f ? toTarget.normalized : Vector2.zero;
            }
        }

        /// <summary>
        /// Current speed magnitude.
        /// </summary>
        public float CurrentSpeed => Velocity.magnitude;

        /// <summary>
        /// Velocity component toward the target (positive = approaching, negative = receding).
        /// </summary>
        public Vector2 ForwardVelocity
        {
            get
            {
                var dir = DirectionToTarget;
                float forwardSpeed = Vector2.Dot(Velocity, dir);
                return dir * forwardSpeed;
            }
        }

        /// <summary>
        /// Velocity component perpendicular to target direction (drift/sideways movement).
        /// </summary>
        public Vector2 LateralVelocity => Velocity - ForwardVelocity;

        /// <summary>
        /// How aligned current velocity is with target direction.
        /// 1.0 = heading toward target, -1.0 = heading away, 0 = perpendicular.
        /// </summary>
        public float VelocityAlignment => CurrentSpeed > 0.01f
            ? Vector2.Dot(Velocity.normalized, DirectionToTarget)
            : 0f;
    }
}
