using UnityEngine;
using Starfire.Entity.AI.Steering;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Calculates steering force for recovering from an overshoot.
    /// Adapts behavior based on current speed:
    /// - Fast: gentle loop back to conserve momentum
    /// - Slow: aggressive reverse correction
    /// </summary>
    public class CalculateRecoveryAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _predictionKey;
        private readonly string _outputKey;
        private readonly float _speedThreshold;
        private readonly float _turnBrakeFactor;

        public CalculateRecoveryAction(
            string targetKey = "steering_target",
            string predictionKey = "trajectory_prediction",
            string outputKey = "steering_force",
            float speedThreshold = 5.0f,
            float turnBrakeFactor = 0.7f)
        {
            _targetKey = targetKey;
            _predictionKey = predictionKey;
            _outputKey = outputKey;
            _speedThreshold = speedThreshold;
            _turnBrakeFactor = turnBrakeFactor;
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

            var ctx = SteeringContext.FromShip(Context.Controller);
            float speed = ctx.Velocity.magnitude;

            Vector2 steeringForce;

            if (speed > _speedThreshold)
            {
                // Moving fast - loop back around
                steeringForce = CalculateLoopBackForce(ctx, target);
            }
            else
            {
                // Moving slow - aggressive correction
                steeringForce = CalculateAggressiveCorrectionForce(ctx, target);
            }

            Context.Set(_outputKey, steeringForce);

            return BTNodeStatus.Success;
        }

        private Vector2 CalculateLoopBackForce(SteeringContext ctx, Vector2 target)
        {
            Vector2 toTarget = target - ctx.Position;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                return Vector2.zero;
            }

            Vector2 targetDir = toTarget.normalized;
            Vector2 velocityDir = ctx.Velocity.normalized;

            // Calculate perpendicular direction for turning
            // Use cross product to determine which way to turn
            float cross = velocityDir.x * targetDir.y - velocityDir.y * targetDir.x;
            Vector2 perpendicular = new Vector2(-velocityDir.y, velocityDir.x);
            if (cross < 0)
            {
                perpendicular = -perpendicular;
            }

            // Blend: turn component + brake component
            Vector2 turnForce = perpendicular * ctx.MaxAcceleration * _turnBrakeFactor;
            Vector2 brakeForce = -velocityDir * ctx.MaxAcceleration * (1f - _turnBrakeFactor);

            Vector2 totalForce = turnForce + brakeForce;

            // Clamp to max acceleration
            if (totalForce.sqrMagnitude > ctx.MaxAcceleration * ctx.MaxAcceleration)
            {
                totalForce = totalForce.normalized * ctx.MaxAcceleration;
            }

            return totalForce;
        }

        private Vector2 CalculateAggressiveCorrectionForce(SteeringContext ctx, Vector2 target)
        {
            float speed = ctx.Velocity.magnitude;

            if (speed > 0.5f)
            {
                // Still moving - brake hard first
                return -ctx.Velocity.normalized * ctx.MaxAcceleration;
            }
            else
            {
                // Nearly stopped - accelerate toward target
                Vector2 toTarget = target - ctx.Position;
                if (toTarget.sqrMagnitude < 0.001f)
                {
                    return Vector2.zero;
                }

                return toTarget.normalized * ctx.MaxAcceleration;
            }
        }
    }
}
