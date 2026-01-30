using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Moves toward a target and slows down to stop at it.
    /// Uses a slowing radius to begin deceleration before reaching the target.
    /// </summary>
    public class ArriveBehavior : ISteeringBehavior
    {
        /// <summary>
        /// The target position to arrive at.
        /// </summary>
        public Vector2 Target { get; set; }

        /// <summary>
        /// Distance at which to start slowing down.
        /// </summary>
        public float SlowingRadius { get; set; } = 5f;

        /// <summary>
        /// Distance at which the agent is considered arrived.
        /// </summary>
        public float ArrivalRadius { get; set; } = 0.5f;

        public SteeringOutput Calculate(SteeringContext ctx)
        {
            Vector2 toTarget = Target - ctx.Position;
            float distance = toTarget.magnitude;

            // Already at target - brake to stop
            if (distance < ArrivalRadius)
            {
                // Apply braking force opposite to current velocity
                Vector2 brakeForce = ctx.Velocity.magnitude > 0.01f
                    ? -ctx.Velocity.normalized * ctx.MaxAcceleration
                    : Vector2.zero;

                return new SteeringOutput
                {
                    DesiredVelocity = Vector2.zero,
                    SteeringForce = brakeForce,
                    ShouldBrake = true,
                    DistanceToTarget = distance
                };
            }

            // Calculate target speed based on distance
            float targetSpeed;
            bool shouldBrake;

            if (distance < SlowingRadius)
            {
                // Inside slowing radius - reduce speed proportionally
                targetSpeed = ctx.MaxSpeed * (distance / SlowingRadius);
                shouldBrake = true;
            }
            else
            {
                // Outside slowing radius - full speed
                targetSpeed = ctx.MaxSpeed;
                shouldBrake = false;
            }

            // Desired velocity toward target at calculated speed
            Vector2 desiredVelocity = toTarget.normalized * targetSpeed;

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
                ShouldBrake = shouldBrake,
                DistanceToTarget = distance
            };
        }
    }
}
