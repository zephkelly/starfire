using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that checks if the entity is within a range of another entity.
    /// Returns Success if within range, Failure otherwise.
    ///
    /// Use for attack range checks, flee distance, engagement zones:
    ///   Selector:
    ///     - Sequence (attack if close):
    ///         - IsInRangeOfEntity (maxRange: 5)
    ///         - FireWeapon
    ///     - Sequence (pursue if far):
    ///         - SetEntityAsTarget
    ///         - CalculateSteering
    /// </summary>
    public class IsInRangeOfEntityCondition : BTAction
    {
        private readonly string _entityKey;
        private readonly float _minRange;
        private readonly float _maxRange;

        public IsInRangeOfEntityCondition(
            string entityKey = "target_entity",
            float minRange = 0f,
            float maxRange = 10f)
        {
            _entityKey = entityKey;
            _minRange = minRange;
            _maxRange = maxRange;
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // Get entity transform from blackboard
            if (!Context.TryGet<Transform>(_entityKey, out var entity) || entity == null)
            {
                return BTNodeStatus.Failure;
            }

            // Calculate distance
            Vector2 currentPos = Context.Transform.position;
            Vector2 targetPos = entity.position;
            float distance = Vector2.Distance(currentPos, targetPos);

            // Check if within range bounds
            bool inRange = distance >= _minRange && distance <= _maxRange;
            return inRange ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}
