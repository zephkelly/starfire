using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Copies a transform's current position to the target key.
    /// Use for tracking moving targets like the player or allies.
    /// Should be called each tick to update the target position.
    /// Blackboard can store Transform, GameObject, or EntityControllerBase.
    /// </summary>
    public class SetTargetFromEntityAction : BTAction
    {
        private readonly string _entityKey;
        private readonly string _targetKey;

        public SetTargetFromEntityAction(string entityKey = "steering_entity", string targetKey = "steering_target")
        {
            _entityKey = entityKey;
            _targetKey = targetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Try to get transform from blackboard (supports multiple types)
            Transform transform = null;

            // Try Transform directly
            if (Context.TryGet<Transform>(_entityKey, out var t))
            {
                transform = t;
            }
            // Try GameObject
            else if (Context.TryGet<GameObject>(_entityKey, out var go))
            {
                transform = go?.transform;
            }
            // Try EntityControllerBase
            else if (Context.TryGet<EntityControllerBase>(_entityKey, out var controller))
            {
                transform = controller?.transform;
            }

            if (transform == null)
            {
                return BTNodeStatus.Failure;
            }

            // Get current position
            Vector2 position = transform.position;
            Context.Set(_targetKey, position);

            return BTNodeStatus.Success;
        }
    }
}
