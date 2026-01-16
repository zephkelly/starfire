using Starfire.Entity.AI.Heuristics;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Calculates steering force to brake while allowing rotation toward target.
    /// Used when ship is facing significantly away from target (high approach angle).
    /// Applies primarily braking force with a small correction toward target.
    /// Returns Success when speed drops below threshold, allowing normal steering to resume.
    /// Reads speed/acceleration from blackboard heuristics.
    /// </summary>
    public class CalculateBrakeAndTurnAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _outputKey;
        private readonly float _brakeFactor;
        private readonly float _minSpeedThreshold;

        public CalculateBrakeAndTurnAction(
            string targetKey = "steering_target",
            string outputKey = "steering_force",
            float brakeFactor = 0.8f,
            float minSpeedThreshold = 2f)
        {
            _targetKey = targetKey;
            _outputKey = outputKey;
            _brakeFactor = Mathf.Clamp01(brakeFactor);
            _minSpeedThreshold = minSpeedThreshold;
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

            // Get propulsion values from heuristics
            Context.TryGet<float>(HeuristicKeys.MaxAcceleration, out var maxAccel);
            Context.TryGet<float>(HeuristicKeys.MaxSpeed, out var maxSpeed);

            Vector2 position = Context.Transform.position;
            Vector2 velocity = Context.Controller.Rigidbody.linearVelocity;
            float speed = velocity.magnitude;

            // If already slow enough, return Failure so selector falls through
            // to calc-smart-arrive which will provide forward thrust toward target
            if (speed < _minSpeedThreshold)
            {
                return BTNodeStatus.Failure;
            }

            // Calculate braking force (opposite to velocity)
            Vector2 brakeForce = -velocity.normalized * maxAccel * _brakeFactor;

            // Correction scales with how slow we are - no swing when fast (like a car backing up)
            Vector2 toTarget = target - position;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Vector2 targetDir = toTarget.normalized;

                // speedRatio: 1.0 at max speed, 0.0 when stopped
                float speedRatio = Mathf.Clamp01(speed / maxSpeed);

                // correctionStrength: 0% when fast, up to (1-brakeFactor)% when slow
                float correctionStrength = (1f - speedRatio) * (1f - _brakeFactor);

                Vector2 correctionForce = targetDir * maxAccel * correctionStrength;
                brakeForce += correctionForce;
            }

            // Clamp to max acceleration
            if (brakeForce.sqrMagnitude > maxAccel * maxAccel)
            {
                brakeForce = brakeForce.normalized * maxAccel;
            }

            Context.Set(_outputKey, brakeForce);
            return BTNodeStatus.Success;
        }
    }
}
