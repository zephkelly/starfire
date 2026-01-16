using Starfire.Entity.AI.Heuristics;
using Starfire.Entity.AI.Steering;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Calculates steering force to pursue a target without deceleration.
    /// Pure pursuit - always accelerates toward target at max acceleration.
    /// Reads speed/acceleration from blackboard heuristics.
    /// </summary>
    public class CalculateSeekAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _outputKey;

        public CalculateSeekAction(string targetKey = "steering_target", string outputKey = "steering_force")
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

            // Calculate seek steering
            Vector2 toTarget = target - ctx.Position;
            Vector2 desiredVelocity = toTarget.normalized * ctx.MaxSpeed;
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
