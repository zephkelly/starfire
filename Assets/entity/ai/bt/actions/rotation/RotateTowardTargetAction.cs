using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Rotates entity to face a target position.
    /// Uses the entity's RotationModule if available.
    /// </summary>
    public class RotateTowardTargetAction : BTAction
    {
        private readonly string _targetKey;

        public RotateTowardTargetAction(string targetKey = "steering_target")
        {
            _targetKey = targetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                return BTNodeStatus.Failure;
            }

            // Calculate direction to target
            Vector2 position = Context.Transform.position;
            Vector2 direction = target - position;

            if (direction.sqrMagnitude < 0.001f)
            {
                return BTNodeStatus.Success; // Already at target
            }

            // Calculate target angle
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Set aim position on AIDriver (world space)
            Context.Driver.AimPosition = target;

            return BTNodeStatus.Success;
        }
    }
}
