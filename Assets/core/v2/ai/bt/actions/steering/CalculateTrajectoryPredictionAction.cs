using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Calculates trajectory prediction data for use by other steering nodes.
    /// Analyzes current velocity relative to target to detect impending misses and overshoots.
    /// </summary>
    public class CalculateTrajectoryPredictionAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _predictionKey;
        private readonly float _missThreshold;

        public CalculateTrajectoryPredictionAction(
            string targetKey = "steering_target",
            string predictionKey = "trajectory_prediction",
            float missThreshold = 2.0f)
        {
            _targetKey = targetKey;
            _predictionKey = predictionKey;
            _missThreshold = missThreshold;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                return BTNodeStatus.Failure;
            }

            // Build steering context
            var ctx = SteeringContext.FromController(Context.Controller);

            // Calculate prediction
            var prediction = CalculatePrediction(ctx.Position, ctx.Velocity, target);

            // Store in blackboard
            Context.Set(_predictionKey, prediction);

            return BTNodeStatus.Success;
        }

        private TrajectoryPrediction CalculatePrediction(Vector2 position, Vector2 velocity, Vector2 target)
        {
            var prediction = new TrajectoryPrediction();

            Vector2 toTarget = target - position;
            float distance = toTarget.magnitude;
            float speed = velocity.magnitude;

            prediction.DistanceToTarget = distance;
            prediction.CurrentSpeed = speed;

            // Handle stationary case
            if (speed < 0.01f)
            {
                prediction.ClosestApproachPoint = position;
                prediction.MissDistance = distance;
                prediction.TimeToClosestApproach = 0f;
                prediction.WillMiss = distance > _missThreshold;
                prediction.ApproachAngle = 0f;
                prediction.LateralSpeed = 0f;
                prediction.RadialSpeed = 0f;
                prediction.RadialVelocity = Vector2.zero;
                prediction.LateralVelocity = Vector2.zero;
                prediction.HasOvershot = false;
                return prediction;
            }

            // Decompose velocity into radial and lateral components
            Vector2 targetDir = distance > 0.001f ? toTarget.normalized : Vector2.up;

            // Radial speed: positive = approaching, negative = receding
            float radialSpeed = Vector2.Dot(velocity, targetDir);
            Vector2 radialVelocity = radialSpeed * targetDir;
            Vector2 lateralVelocity = velocity - radialVelocity;
            float lateralSpeed = lateralVelocity.magnitude;

            prediction.RadialSpeed = radialSpeed;
            prediction.RadialVelocity = radialVelocity;
            prediction.LateralSpeed = lateralSpeed;
            prediction.LateralVelocity = lateralVelocity;

            // Approach angle: angle between velocity and target direction
            Vector2 velocityDir = velocity.normalized;
            float dot = Mathf.Clamp(Vector2.Dot(velocityDir, targetDir), -1f, 1f);
            prediction.ApproachAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;

            // Closest approach point calculation
            // Line: P + t * V, find t where distance to T is minimized
            // t = dot(toTarget, velocity) / |velocity|^2
            float t = Vector2.Dot(toTarget, velocity) / velocity.sqrMagnitude;
            t = Mathf.Max(0f, t); // Can't go back in time

            Vector2 closestPoint = position + velocity * t;
            float missDistance = Vector2.Distance(closestPoint, target);

            prediction.ClosestApproachPoint = closestPoint;
            prediction.MissDistance = missDistance;
            prediction.TimeToClosestApproach = t;

            // Will miss if closest approach is farther than threshold
            prediction.WillMiss = missDistance > _missThreshold && t > 0.01f;

            // Has overshot if moving away from target and not too close
            prediction.HasOvershot = radialSpeed < -0.1f && distance > _missThreshold * 0.5f;

            return prediction;
        }
    }
}
