using UnityEngine;

namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Seeks toward a target at maximum speed.
    /// Does not slow down when approaching - use ArriveBehavior for that.
    /// </summary>
    public class SeekBehavior : ISteeringBehavior
    {
        /// <summary>
        /// The target position to seek toward.
        /// </summary>
        public Vector2 Target { get; set; }

        public SteeringOutput Calculate(SteeringContext ctx)
        {
            Vector2 toTarget = Target - ctx.Position;
            float distance = toTarget.magnitude;

            // Desired velocity points at target at max speed
            Vector2 desiredVelocity = distance > 0.001f
                ? toTarget.normalized * ctx.MaxSpeed
                : Vector2.zero;

            // Steering = desired - current
            Vector2 steering = desiredVelocity - ctx.Velocity;

            // Clamp to max acceleration
            if (steering.magnitude > ctx.MaxAcceleration)
            {
                steering = steering.normalized * ctx.MaxAcceleration;
            }

            return new SteeringOutput
            {
                DesiredVelocity = desiredVelocity,
                SteeringForce = steering,
                ShouldBrake = false,
                DistanceToTarget = distance
            };
        }
    }
}
