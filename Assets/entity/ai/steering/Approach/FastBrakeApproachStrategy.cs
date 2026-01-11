using UnityEngine;

namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Fast brake approach strategy: full throttle until calculated braking point, then hard brake.
    /// Best for: Urgent travel, time-critical objectives.
    /// </summary>
    public class FastBrakeApproachStrategy : IApproachStrategy
    {
        /// <summary>
        /// Safety buffer added to stopping distance calculation (in units).
        /// Higher values start braking earlier.
        /// </summary>
        public float BrakingBuffer { get; set; } = 0.5f;

        public SteeringOutput Calculate(ApproachContext ctx)
        {
            Vector2 toTarget = ctx.TargetWaypoint - ctx.Position;
            float distance = toTarget.magnitude;

            // Calculate stopping distance: v² / (2 * a)
            // This is the distance needed to stop from current speed with max deceleration
            float currentSpeed = ctx.Velocity.magnitude;
            float stoppingDistance = (currentSpeed * currentSpeed) / (2f * ctx.MaxAcceleration);

            // Determine if we're heading toward the target
            Vector2 targetDirection = distance > 0.001f ? toTarget.normalized : Vector2.zero;
            float approachAngle = Vector2.Dot(ctx.Velocity.normalized, targetDirection);

            // If we're past the target or very close, apply braking
            if (distance <= ctx.ArrivalThreshold)
            {
                Vector2 brakeForce = currentSpeed > 0.01f
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

            // Should we start braking?
            // Brake if: distance to target <= stopping distance + buffer
            // Also consider if we're not heading toward the target (need to correct course first)
            bool shouldBrake = distance <= stoppingDistance + BrakingBuffer && approachAngle > 0.5f;

            if (shouldBrake)
            {
                // BRAKE: Apply full reverse thrust opposite to velocity
                Vector2 brakeForce = currentSpeed > 0.01f
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
            else
            {
                // ACCELERATE: Full thrust toward target with lateral velocity correction
                Vector2 desiredVelocity = targetDirection * ctx.MaxSpeed;

                // Decompose current velocity into forward and lateral components
                Vector2 lateralVelocity = ctx.LateralVelocity;
                Vector2 forwardVelocity = ctx.ForwardVelocity;

                // Calculate correction forces
                Vector2 lateralCorrection = -lateralVelocity;  // Cancel sideways drift
                Vector2 forwardSteering = desiredVelocity - forwardVelocity;

                // Weight lateral correction - more aggressive for fast brake since we want precise stopping
                float lateralMagnitude = lateralVelocity.magnitude;
                float lateralRatio = lateralMagnitude / Mathf.Max(ctx.CurrentSpeed, 0.1f);
                float correctionWeight = Mathf.Clamp01(lateralRatio * 2.5f);  // More aggressive than controlled

                // Prioritize lateral correction when drifting
                Vector2 steering = lateralCorrection + forwardSteering * (1f - correctionWeight * 0.6f);

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

        public bool IsArrived(ApproachContext ctx, SteeringOutput output)
        {
            // Fast brake approach: must be within distance AND nearly stopped
            return output.DistanceToTarget <= ctx.ArrivalThreshold
                && ctx.Velocity.magnitude < ctx.VelocityThreshold;
        }
    }
}
