using UnityEngine;

namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Controlled approach strategy: physics-based braking for zero-drag space.
    /// Uses exact stopping distance calculation to determine when to apply full reverse thrust.
    /// Best for: Patrol, docking, precise positioning.
    /// </summary>
    public class ControlledApproachStrategy : IApproachStrategy
    {
        private const bool DEBUG_LOG = true;
        private string _lastState = "";
        private int _frameCount = 0;

        public SteeringOutput Calculate(ApproachContext ctx)
        {
            Vector2 toTarget = ctx.TargetWaypoint - ctx.Position;
            float distance = toTarget.magnitude;
            Vector2 targetDir = distance > 0.001f ? toTarget.normalized : Vector2.zero;

            float forwardSpeed = Vector2.Dot(ctx.Velocity, targetDir);
            float speed = ctx.Velocity.magnitude;

            // Pure physics stopping distance: v² / (2a)
            float stoppingDistance = (forwardSpeed * forwardSpeed) / (2f * ctx.MaxAcceleration);
            float brakeDistance = distance - ctx.ArrivalThreshold;

            _frameCount++;

            // STATE 1: Arrived - hold position
            // Inside arrival radius: accept 2x velocity threshold (more lenient for patrol flow)
            if (distance <= ctx.EffectiveArrivalDistance && speed < ctx.EffectiveVelocityThreshold * 2f)
            {
                return new SteeringOutput
                {
                    DesiredVelocity = Vector2.zero,
                    SteeringForce = Vector2.zero,
                    ShouldBrake = true,
                    DistanceToTarget = distance
                };
            }

            // STATE 2: Settling - close to target with low velocity
            // Settling zone is slightly larger than ARRIVED zone (2x distance, 1.5x velocity of effective thresholds)
            if (speed < ctx.EffectiveVelocityThreshold * 1.5f && distance < ctx.EffectiveArrivalDistance * 2f)
            {
                // Counter-velocity force (damping) to stop the ship
                // Use bounded coefficient that scales reasonably with acceleration
                float dampingCoefficient = Mathf.Min(ctx.MaxAcceleration * 0.15f, 15f);
                Vector2 dampingForce = -ctx.Velocity * dampingCoefficient;

                // Small correction toward target if not quite there
                Vector2 seekForce = targetDir * Mathf.Min(distance, ctx.MaxAcceleration * 0.3f);

                Vector2 force = dampingForce + seekForce;
                if (force.magnitude > ctx.MaxAcceleration)
                {
                    force = force.normalized * ctx.MaxAcceleration;
                }

                return new SteeringOutput
                {
                    DesiredVelocity = Vector2.zero,
                    SteeringForce = force,
                    ShouldBrake = true,
                    DistanceToTarget = distance
                };
            }

            // STATE 3: Braking - need to slow down (approaching or overshooting)
            // Brake if: stopping distance exceeds available distance OR moving away from target
            bool movingTowardTarget = forwardSpeed > 0.01f;
            bool needsToStop = stoppingDistance >= brakeDistance - 0.5f;
            bool movingAwayFromTarget = forwardSpeed < -0.01f;

            if ((needsToStop && movingTowardTarget) || movingAwayFromTarget)
            {
                // When moving away from target, brake against current velocity (not toward target)
                Vector2 brakeDir = movingAwayFromTarget
                    ? -ctx.Velocity.normalized  // Brake against movement direction
                    : -targetDir;               // Brake against approach direction

                Vector2 brakeForce = brakeDir * ctx.MaxAcceleration;

                // Also correct lateral drift while braking (only when moving toward target)
                if (movingTowardTarget)
                {
                    Vector2 lateralVelocity = ctx.Velocity - targetDir * forwardSpeed;
                    if (lateralVelocity.sqrMagnitude > 0.01f)
                    {
                        Vector2 lateralCorrection = -lateralVelocity;
                        if (lateralCorrection.magnitude > ctx.MaxAcceleration * 0.3f)
                        {
                            lateralCorrection = lateralCorrection.normalized * ctx.MaxAcceleration * 0.3f;
                        }
                        brakeForce += lateralCorrection;

                        if (brakeForce.magnitude > ctx.MaxAcceleration)
                        {
                            brakeForce = brakeForce.normalized * ctx.MaxAcceleration;
                        }
                    }
                }

                string brakeType = movingAwayFromTarget ? "BRAKE-REV" : "BRAKING";
                return new SteeringOutput
                {
                    DesiredVelocity = Vector2.zero,
                    SteeringForce = brakeForce,
                    ShouldBrake = true,
                    DistanceToTarget = distance
                };
            }

            // STATE 4: Accelerating - plenty of stopping room
            Vector2 desiredVelocity = targetDir * ctx.MaxSpeed;
            Vector2 steering = desiredVelocity - ctx.Velocity;

            // ANTICIPATORY COUNTER-STEERING
            // Predict where we'll be if we continue on current trajectory
            float lookaheadTime = distance / Mathf.Max(speed, 1f);
            Vector2 projectedPosition = ctx.Position + ctx.Velocity * lookaheadTime;
            Vector2 projectedToTarget = ctx.TargetWaypoint - projectedPosition;
            float projectedMissDistance = projectedToTarget.magnitude;

            // If we'll miss the waypoint, apply aggressive correction
            if (projectedMissDistance > ctx.EffectiveArrivalDistance * 0.5f)
            {
                // Calculate correction direction: steer toward waypoint from projected position
                Vector2 correctionDirection = projectedToTarget.normalized;

                // Scale correction by how badly we'll miss (larger miss = stronger correction)
                float missRatio = projectedMissDistance / ctx.EffectiveArrivalDistance;
                float correctionMagnitude = Mathf.Clamp(missRatio, 0.5f, 2f);

                // Urgency increases as we get closer
                float urgency = Mathf.Clamp01(1f - (distance / (ctx.MaxSpeed * 3f)));

                // Apply strong correction force toward the waypoint
                Vector2 correctionForce = correctionDirection * ctx.MaxAcceleration * correctionMagnitude * urgency;

                // Also dampen existing lateral velocity to stop the drift
                Vector2 lateralDamping = -ctx.LateralVelocity * 0.5f;

                // Blend: prioritize correction over forward thrust when missing badly
                float correctionWeight = Mathf.Clamp01(missRatio * urgency);
                steering = Vector2.Lerp(steering, correctionForce + lateralDamping, correctionWeight);
            }

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

        public bool IsArrived(ApproachContext ctx, SteeringOutput output)
        {
            // Inside arrival radius: accept 2x velocity threshold (more lenient for patrol flow)
            return output.DistanceToTarget <= ctx.EffectiveArrivalDistance
                && ctx.Velocity.magnitude < ctx.EffectiveVelocityThreshold * 2f;
        }
    }
}
