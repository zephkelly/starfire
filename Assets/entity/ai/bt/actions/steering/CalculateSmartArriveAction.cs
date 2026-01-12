using UnityEngine;
using Starfire.Entity.AI.Steering;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Calculates optimal arrival thrust to reach a target with zero velocity.
    /// Uses physics-based trajectory prediction to account for lateral velocity
    /// and calculate the exact thrust vector needed for smooth arrival.
    /// Respects cruise speed/acceleration limits set on the blackboard.
    /// </summary>
    public class CalculateSmartArriveAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _predictionKey;
        private readonly string _outputKey;
        private readonly float _arrivalThreshold;
        private readonly float _angleStiffness;
        private readonly float _lateralBrakingFactor;
        private readonly float _closeRangeThreshold;
        private readonly float _alignmentAngle;
        private readonly string _cruiseSpeedKey;
        private readonly string _cruiseAccelKey;

        public CalculateSmartArriveAction(
            string targetKey = "steering_target",
            string predictionKey = "trajectory_prediction",
            string outputKey = "steering_force",
            float arrivalThreshold = 1.0f,
            float angleStiffness = 1.5f,
            float lateralBrakingFactor = 2.0f,
            float closeRangeThreshold = 10f,
            float alignmentAngle = 30f,
            string cruiseSpeedKey = "cruise_speed",
            string cruiseAccelKey = "cruise_acceleration")
        {
            _targetKey = targetKey;
            _predictionKey = predictionKey;
            _outputKey = outputKey;
            _arrivalThreshold = arrivalThreshold;
            _angleStiffness = angleStiffness;
            _lateralBrakingFactor = lateralBrakingFactor;
            _closeRangeThreshold = closeRangeThreshold;
            _alignmentAngle = alignmentAngle;
            _cruiseSpeedKey = cruiseSpeedKey;
            _cruiseAccelKey = cruiseAccelKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Validate propulsion capability
            var propulsion = Context.Systems?.Propulsion?.Module;
            if (propulsion == null)
            {
                return BTNodeStatus.Failure;
            }

            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                return BTNodeStatus.Failure;
            }

            // Get prediction data (optional - calculate inline if missing)
            TrajectoryPrediction prediction;
            if (!Context.TryGet<TrajectoryPrediction>(_predictionKey, out prediction))
            {
                // Calculate prediction inline if not available
                var ctx = GetSteeringContext();
                prediction = CalculatePredictionInline(ctx.Position, ctx.Velocity, target);
            }

            Vector2 steeringForce = CalculateSteeringForce(target, prediction);

            Context.Set(_outputKey, steeringForce);

            return BTNodeStatus.Success;
        }

        /// <summary>
        /// Gets SteeringContext with cruise speed/acceleration overrides from blackboard.
        /// </summary>
        private SteeringContext GetSteeringContext()
        {
            // Read cruise overrides from blackboard (null if not set or -1)
            float? cruiseSpeed = null;
            float? cruiseAccel = null;

            if (Context.TryGet<float>(_cruiseSpeedKey, out var speed) && speed > 0)
            {
                cruiseSpeed = speed;
            }

            if (Context.TryGet<float>(_cruiseAccelKey, out var accel) && accel > 0)
            {
                cruiseAccel = accel;
            }

            return SteeringContext.FromShip(Context.Controller, cruiseSpeed, cruiseAccel);
        }

        private Vector2 CalculateSteeringForce(Vector2 target, TrajectoryPrediction prediction)
        {
            var ctx = GetSteeringContext();
            float speed = prediction.CurrentSpeed;
            float distance = prediction.DistanceToTarget;

            // Already arrived and stopped
            if (distance <= _arrivalThreshold && speed < 0.5f)
            {
                return Vector2.zero;
            }

            // Use optimal arrival thrust for all approach scenarios
            Vector2 steeringForce = CalculateOptimalArrivalThrust(ctx, target, prediction);

            // Apply turn-then-burn thrust reduction when close to target
            steeringForce = ApplyTurnThenBurnReduction(steeringForce, distance, prediction.ApproachAngle);

            return steeringForce;
        }

        /// <summary>
        /// Reduces thrust when close to target and not facing it.
        /// This allows the ship to rotate toward the target before applying full thrust.
        /// </summary>
        private Vector2 ApplyTurnThenBurnReduction(Vector2 steeringForce, float distance, float approachAngle)
        {
            // Skip if turn-then-burn is disabled
            if (_closeRangeThreshold <= 0f)
            {
                return steeringForce;
            }

            // Only apply when within close range threshold
            if (distance >= _closeRangeThreshold)
            {
                return steeringForce;
            }

            // Full thrust if well-aligned
            if (approachAngle <= _alignmentAngle)
            {
                return steeringForce;
            }

            // Calculate thrust multiplier based on alignment angle
            // Proportional: alignmentAngle° = 100% thrust, 180° = 0% thrust
            float normalizedAngle = (approachAngle - _alignmentAngle) / (180f - _alignmentAngle);
            float thrustMultiplier = 1f - normalizedAngle;
            thrustMultiplier = Mathf.Clamp01(thrustMultiplier);

            return steeringForce * thrustMultiplier;
        }

        /// <summary>
        /// Calculates the optimal thrust vector to arrive at target with zero velocity.
        /// Accounts for lateral velocity by reducing effective distance.
        /// </summary>
        private Vector2 CalculateOptimalArrivalThrust(SteeringContext ctx, Vector2 target, TrajectoryPrediction prediction)
        {
            Vector2 toTarget = target - ctx.Position;
            float distance = prediction.DistanceToTarget;

            if (distance < 0.001f)
            {
                // At target - just brake
                if (prediction.CurrentSpeed > 0.1f)
                {
                    return -ctx.Velocity.normalized * ctx.MaxAcceleration;
                }
                return Vector2.zero;
            }

            Vector2 targetDir = toTarget.normalized;
            float radialSpeed = prediction.RadialSpeed;
            float lateralSpeed = prediction.LateralSpeed;

            // Calculate time needed to cancel lateral velocity
            // During this time, we're still approaching the target radially
            float timeToKillLateral = lateralSpeed / ctx.MaxAcceleration;
            float distanceConsumedByLateral = Mathf.Abs(radialSpeed) * timeToKillLateral;

            // Effective distance remaining after lateral correction
            float effectiveDistance = distance - distanceConsumedByLateral;

            // Calculate stopping distance for current radial speed
            float radialStoppingDist = (radialSpeed * radialSpeed) / (2f * ctx.MaxAcceleration);

            // Check if we need emergency braking
            // Emergency if: effective distance is negative OR we can't stop in time
            bool needsEmergencyBrake = effectiveDistance <= _arrivalThreshold ||
                                       (radialSpeed > 0 && radialStoppingDist >= effectiveDistance);

            if (needsEmergencyBrake && prediction.CurrentSpeed > 0.1f)
            {
                // EMERGENCY: Apply maximum braking opposite to velocity
                return -ctx.Velocity.normalized * ctx.MaxAcceleration;
            }

            // Calculate ideal velocity for current position
            // This is the velocity we SHOULD have to arrive perfectly at zero velocity
            float idealRadialSpeed;
            if (effectiveDistance > 0)
            {
                idealRadialSpeed = Mathf.Sqrt(2f * ctx.MaxAcceleration * effectiveDistance);
                // Cap at max speed
                idealRadialSpeed = Mathf.Min(idealRadialSpeed, ctx.MaxSpeed);
            }
            else
            {
                // Effective distance consumed - should be stopping
                idealRadialSpeed = 0f;
            }

            // Ideal velocity points toward target at the calculated speed
            // Note: ideal velocity has NO lateral component - this naturally corrects drift
            Vector2 idealVelocity = targetDir * idealRadialSpeed;

            // Required thrust to match ideal velocity
            Vector2 requiredDeltaV = idealVelocity - ctx.Velocity;

            // Apply angle stiffness to boost lateral correction when needed
            if (lateralSpeed > 0.5f)
            {
                // Decompose required thrust into radial and lateral components
                float requiredRadial = Vector2.Dot(requiredDeltaV, targetDir);
                Vector2 requiredRadialV = requiredRadial * targetDir;
                Vector2 requiredLateralV = requiredDeltaV - requiredRadialV;

                // Boost lateral component for faster correction
                requiredDeltaV = requiredRadialV + requiredLateralV * _angleStiffness;
            }

            // Clamp to max acceleration
            if (requiredDeltaV.sqrMagnitude > ctx.MaxAcceleration * ctx.MaxAcceleration)
            {
                return requiredDeltaV.normalized * ctx.MaxAcceleration;
            }

            return requiredDeltaV;
        }

        private TrajectoryPrediction CalculatePredictionInline(Vector2 position, Vector2 velocity, Vector2 target)
        {
            var prediction = new TrajectoryPrediction();

            Vector2 toTarget = target - position;
            float distance = toTarget.magnitude;
            float speed = velocity.magnitude;

            prediction.DistanceToTarget = distance;
            prediction.CurrentSpeed = speed;

            if (speed < 0.01f)
            {
                prediction.HasOvershot = false;
                return prediction;
            }

            Vector2 targetDir = distance > 0.001f ? toTarget.normalized : Vector2.up;

            float radialSpeed = Vector2.Dot(velocity, targetDir);
            prediction.RadialSpeed = radialSpeed;
            prediction.RadialVelocity = radialSpeed * targetDir;
            prediction.LateralVelocity = velocity - prediction.RadialVelocity;
            prediction.LateralSpeed = prediction.LateralVelocity.magnitude;

            float dot = Mathf.Clamp(Vector2.Dot(velocity.normalized, targetDir), -1f, 1f);
            prediction.ApproachAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;

            float t = Vector2.Dot(toTarget, velocity) / velocity.sqrMagnitude;
            t = Mathf.Max(0f, t);
            prediction.ClosestApproachPoint = position + velocity * t;
            prediction.MissDistance = Vector2.Distance(prediction.ClosestApproachPoint, target);
            prediction.WillMiss = prediction.MissDistance > 2f && t > 0.01f;
            prediction.HasOvershot = radialSpeed < -0.1f && distance > 1f;

            return prediction;
        }
    }
}
