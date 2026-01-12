using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when entity is within threshold distance of target.
    /// </summary>
    public class IsNearPositionCondition : BTLeafCondition
    {
        private readonly string _targetKey;
        private readonly float _threshold;

        public IsNearPositionCondition(string targetKey = "steering_target", float threshold = 1.0f)
        {
            _targetKey = targetKey;
            _threshold = threshold;
        }

        protected override bool CheckCondition()
        {
            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                Debug.LogWarning($"[IsNearPosition] No target in blackboard key '{_targetKey}'");
                return false;
            }

            // Check distance
            Vector2 position = Context.Transform.position;
            float distance = Vector2.Distance(position, target);
            bool result = distance <= _threshold;

            return result;
        }
    }
}
