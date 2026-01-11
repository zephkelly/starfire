using UnityEngine;

namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Flyby approach strategy: maintain speed through waypoint, angle trajectory toward next waypoint.
    /// Best for: Racing, smooth multi-waypoint paths.
    /// </summary>
    public class FlybyApproachStrategy : IApproachStrategy
    {
        /// <summary>
        /// Distance from waypoint at which to start blending toward next waypoint direction.
        /// </summary>
        public float TransitionRadius { get; set; } = 10f;

        /// <summary>
        /// Minimum speed to maintain during flyby (as fraction of max speed).
        /// </summary>
        public float MinSpeedFraction { get; set; } = 0.5f;

        public SteeringOutput Calculate(ApproachContext ctx)
        {
            Vector2 toTarget = ctx.TargetWaypoint - ctx.Position;
            float distance = toTarget.magnitude;
            Vector2 targetDirection = distance > 0.001f ? toTarget.normalized : Vector2.zero;

            Vector2 desiredVelocity;

            // If we have a next waypoint and we're close to current target, start transitioning
            if (ctx.NextWaypoint.HasValue && distance < TransitionRadius)
            {
                // Calculate direction to next waypoint from current waypoint
                Vector2 toNextWaypoint = ctx.NextWaypoint.Value - ctx.TargetWaypoint;
                Vector2 nextDirection = toNextWaypoint.magnitude > 0.001f
                    ? toNextWaypoint.normalized
                    : targetDirection;

                // Blend between current target direction and next waypoint direction
                // As we get closer, blend more toward the next waypoint
                float blendFactor = 1f - (distance / TransitionRadius);
                blendFactor = Mathf.Clamp01(blendFactor);

                // Smooth blend using squared factor for more gradual transition
                blendFactor = blendFactor * blendFactor;

                Vector2 blendedDirection = Vector2.Lerp(targetDirection, nextDirection, blendFactor).normalized;

                // Maintain full speed through the waypoint
                desiredVelocity = blendedDirection * ctx.MaxSpeed;
            }
            else
            {
                // No next waypoint or too far from current target - just seek current target
                desiredVelocity = targetDirection * ctx.MaxSpeed;
            }

            // Decompose velocity relative to desired direction (not target direction)
            // This is different from other strategies because flyby blends toward next waypoint
            Vector2 desiredDir = desiredVelocity.normalized;
            float forwardSpeed = Vector2.Dot(ctx.Velocity, desiredDir);
            Vector2 forwardVelocity = desiredDir * forwardSpeed;
            Vector2 lateralVelocity = ctx.Velocity - forwardVelocity;

            // Calculate correction forces
            Vector2 lateralCorrection = -lateralVelocity;
            Vector2 forwardSteering = desiredVelocity - forwardVelocity;

            // Flyby uses gentler lateral correction - we want smooth curves, not sharp corrections
            float lateralMagnitude = lateralVelocity.magnitude;
            float lateralRatio = lateralMagnitude / Mathf.Max(ctx.CurrentSpeed, 0.1f);
            float correctionWeight = Mathf.Clamp01(lateralRatio * 1.5f);  // Gentler than other strategies

            // Blend with emphasis on smooth forward motion
            Vector2 steering = forwardSteering + lateralCorrection * (0.5f + correctionWeight * 0.5f);

            // Clamp to max acceleration
            if (steering.magnitude > ctx.MaxAcceleration)
            {
                steering = steering.normalized * ctx.MaxAcceleration;
            }

            return new SteeringOutput
            {
                DesiredVelocity = desiredVelocity,
                SteeringForce = steering,
                ShouldBrake = false, // Flyby never brakes
                DistanceToTarget = distance
            };
        }

        public bool IsArrived(ApproachContext ctx, SteeringOutput output)
        {
            // Flyby: arrived when we pass through the waypoint (distance only, no velocity check)
            // We're "through" when we're within the arrival threshold
            return output.DistanceToTarget <= ctx.ArrivalThreshold;
        }
    }
}
