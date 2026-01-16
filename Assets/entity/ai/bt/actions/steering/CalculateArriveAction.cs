using Starfire.Entity.AI.Heuristics;
using Starfire.Entity.AI.Steering;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Calculates steering force to arrive at a target with proper deceleration.
    /// Uses physics-based stopping distance: v² / (2a)
    /// Reads speed/acceleration from blackboard heuristics.
    /// </summary>
    public class CalculateArriveAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _outputKey;
        private readonly float _arrivalThreshold;

        public CalculateArriveAction(
            string targetKey = "steering_target",
            string outputKey = "steering_force",
            float arrivalThreshold = 1.0f)
        {
            _targetKey = targetKey;
            _outputKey = outputKey;
            _arrivalThreshold = arrivalThreshold;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Validate propulsion capability via perception layer
            if (!Context.TryGet<bool>(HeuristicKeys.HasPropulsionModule, out var hasPropulsion) || !hasPropulsion)
            {
                Debug.LogWarning($"[CalculateArrive] FAILURE: No propulsion module (heuristic)");
                return BTNodeStatus.Failure;
            }

            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                Debug.LogWarning($"[CalculateArrive] FAILURE: No target found in blackboard key '{_targetKey}'");
                return BTNodeStatus.Failure;
            }

            // Build steering context from blackboard heuristics
            var ctx = SteeringContext.FromBlackboard(Context.Controller, Context);

            // Calculate distance and current speed
            Vector2 toTarget = target - ctx.Position;
            float distance = toTarget.magnitude;
            float speed = ctx.Velocity.magnitude;

            Vector2 steeringForce;
            string mode;

            // Physics-based stopping distance: v² / (2a)
            float stoppingDistance = (speed * speed) / (2f * ctx.MaxAcceleration);

            if (distance <= _arrivalThreshold && speed < 0.5f)
            {
                // Already arrived and nearly stopped - zero force
                steeringForce = Vector2.zero;
                mode = "ARRIVED";
            }
            else if (stoppingDistance >= distance - _arrivalThreshold)
            {
                // Need to brake - apply force opposite to velocity
                if (speed > 0.1f)
                {
                    steeringForce = -ctx.Velocity.normalized * ctx.MaxAcceleration;
                    mode = "BRAKING";
                }
                else
                {
                    steeringForce = Vector2.zero;
                    mode = "STOPPED";
                }
            }
            else
            {
                // Not yet in braking zone - seek toward target
                Vector2 desiredVelocity = toTarget.normalized * ctx.MaxSpeed;
                steeringForce = desiredVelocity - ctx.Velocity;

                // Clamp to max acceleration
                if (steeringForce.sqrMagnitude > ctx.MaxAcceleration * ctx.MaxAcceleration)
                {
                    steeringForce = steeringForce.normalized * ctx.MaxAcceleration;
                }
                mode = "SEEKING";
            }

            // Write result to blackboard
            Context.Set(_outputKey, steeringForce);

            Debug.Log($"[CalculateArrive] {mode}: pos={ctx.Position}, target={target}, dist={distance:F1}, speed={speed:F1}, stopDist={stoppingDistance:F1}, force={steeringForce}, maxAccel={ctx.MaxAcceleration}, maxSpeed={ctx.MaxSpeed}");

            return BTNodeStatus.Success;
        }
    }
}
