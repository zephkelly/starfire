using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Rotates entity to face its movement direction.
    /// Uses the entity's RotationModule if available.
    /// </summary>
    public class RotateTowardVelocityAction : BTAction
    {
        private readonly float _minSpeedThreshold;

        public RotateTowardVelocityAction(float minSpeedThreshold = 0.5f)
        {
            _minSpeedThreshold = minSpeedThreshold;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            if (Context.Controller?.Rigidbody == null)
            {
                return BTNodeStatus.Failure;
            }

            Vector2 velocity = Context.Controller.Rigidbody.linearVelocity;
            float speed = velocity.magnitude;

            // Don't rotate if moving too slowly
            if (speed < _minSpeedThreshold)
            {
                return BTNodeStatus.Success;
            }

            // Calculate aim position ahead of current position in velocity direction
            Vector2 position = Context.Transform.position;
            Vector2 aimTarget = position + velocity.normalized * 10f; // Look 10 units ahead

            // Set aim position on AIDriver (world space)
            Context.Driver.AimPosition = aimTarget;

            return BTNodeStatus.Success;
        }
    }
}
