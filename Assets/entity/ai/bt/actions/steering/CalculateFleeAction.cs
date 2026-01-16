using Starfire.Entity.AI.Heuristics;
using Starfire.Entity.AI.Steering;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Calculates steering force to flee away from a target position.
    /// Accelerates in the opposite direction from target.
    /// Reads speed/acceleration from blackboard heuristics.
    /// </summary>
    public class CalculateFleeAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _outputKey;

        public CalculateFleeAction(string targetKey = "steering_target", string outputKey = "steering_force")
        {
            _targetKey = targetKey;
            _outputKey = outputKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Validate propulsion capability via perception layer
            if (!Context.TryGet<bool>(HeuristicKeys.HasPropulsionModule, out var hasPropulsion) || !hasPropulsion)
            {
                return BTNodeStatus.Failure;
            }

            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                return BTNodeStatus.Failure;
            }

            // Build steering context from blackboard heuristics
            var ctx = SteeringContext.FromBlackboard(Context.Controller, Context);

            // Calculate flee steering (opposite of seek)
            Vector2 fromTarget = ctx.Position - target;

            // If we're exactly on the target, pick a random direction
            if (fromTarget.sqrMagnitude < 0.001f)
            {
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                fromTarget = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            }

            Vector2 desiredVelocity = fromTarget.normalized * ctx.MaxSpeed;
            Vector2 steeringForce = desiredVelocity - ctx.Velocity;

            // Clamp to max acceleration
            if (steeringForce.sqrMagnitude > ctx.MaxAcceleration * ctx.MaxAcceleration)
            {
                steeringForce = steeringForce.normalized * ctx.MaxAcceleration;
            }

            // Write result to blackboard
            Context.Set(_outputKey, steeringForce);

            return BTNodeStatus.Success;
        }
    }
}
