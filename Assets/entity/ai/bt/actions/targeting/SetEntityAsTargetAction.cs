using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Sets a dynamic entity's position as the move target.
    /// Reads a Transform from blackboard and writes its current position to the target key.
    ///
    /// Use for chase/follow behaviors where the target is moving:
    ///   Sequence:
    ///     - SetEntityAsTarget (updates move_target each frame)
    ///     - CalculateSteering
    ///     - ApplyMovement
    /// </summary>
    public class SetEntityAsTargetAction : BTAction
    {
        private readonly string _entityKey;
        private readonly string _targetKey;

        public SetEntityAsTargetAction(
            string entityKey = "target_entity",
            string targetKey = "move_target")
        {
            _entityKey = entityKey;
            _targetKey = targetKey;
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // Get entity transform from blackboard
            if (!Context.TryGet<Transform>(_entityKey, out var entity) || entity == null)
            {
                Debug.LogWarning($"[SetEntityAsTarget] No entity found at key '{_entityKey}'");
                return BTNodeStatus.Failure;
            }

            // Write current position as target
            Vector2 targetPosition = entity.position;
            Context.Set(_targetKey, targetPosition);

            return BTNodeStatus.Success;
        }
    }
}
