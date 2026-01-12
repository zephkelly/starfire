using UnityEngine;
using Starfire.Entity.AI.Steering;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Calculates steering for fly-through waypoints where stopping is not required.
    /// Ship passes within a specified radius without decelerating to a stop.
    /// </summary>
    public class CalculateFlyThroughAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _predictionKey;
        private readonly string _outputKey;
        private readonly float _passRadius;

        public CalculateFlyThroughAction(
            string targetKey = "steering_target",
            string predictionKey = "trajectory_prediction",
            string outputKey = "steering_force",
            float passRadius = 3.0f)
        {
            _targetKey = targetKey;
            _predictionKey = predictionKey;
            _outputKey = outputKey;
            _passRadius = passRadius;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Validate propulsion capability
            var propulsion = Context.Systems?.PrimaryImpulse;
            if (propulsion == null)
            {
                return BTNodeStatus.Failure;
            }

            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                return BTNodeStatus.Failure;
            }

            var ctx = SteeringContext.FromShip(Context.Controller);

            Vector2 toTarget = target - ctx.Position;
            float distance = toTarget.magnitude;

            // Already within pass radius - maintain course
            if (distance < _passRadius)
            {
                Context.Set(_outputKey, Vector2.zero);
                return BTNodeStatus.Success;
            }

            // Get prediction if available
            TrajectoryPrediction prediction;
            if (Context.TryGet<TrajectoryPrediction>(_predictionKey, out prediction))
            {
                // Check if current trajectory will pass within radius
                if (prediction.MissDistance <= _passRadius && prediction.TimeToClosestApproach > 0)
                {
                    // Will pass within radius - maintain course
                    Context.Set(_outputKey, Vector2.zero);
                    return BTNodeStatus.Success;
                }
            }

            // Need course correction - use seek behavior
            Vector2 steeringForce = CalculateSeekForce(ctx, target);
            Context.Set(_outputKey, steeringForce);

            return BTNodeStatus.Success;
        }

        private Vector2 CalculateSeekForce(SteeringContext ctx, Vector2 target)
        {
            Vector2 toTarget = target - ctx.Position;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                return Vector2.zero;
            }

            Vector2 desiredVelocity = toTarget.normalized * ctx.MaxSpeed;
            Vector2 steering = desiredVelocity - ctx.Velocity;

            // Clamp to max acceleration
            if (steering.sqrMagnitude > ctx.MaxAcceleration * ctx.MaxAcceleration)
            {
                steering = steering.normalized * ctx.MaxAcceleration;
            }

            return steering;
        }
    }
}
