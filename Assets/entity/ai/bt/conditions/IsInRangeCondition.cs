using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when entity is within min/max range of target.
    /// </summary>
    public class IsInRangeCondition : BTLeafCondition
    {
        private readonly string _targetKey;
        private readonly float _minRange;
        private readonly float _maxRange;

        public IsInRangeCondition(string targetKey = "steering_target", float minRange = 0f, float maxRange = 50f)
        {
            _targetKey = targetKey;
            _minRange = minRange;
            _maxRange = maxRange;
        }

        protected override bool CheckCondition()
        {
            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                return false;
            }

            // Calculate distance
            Vector2 position = Context.Transform.position;
            float distance = Vector2.Distance(position, target);

            // Check range bounds
            return distance >= _minRange && distance <= _maxRange;
        }
    }
}
