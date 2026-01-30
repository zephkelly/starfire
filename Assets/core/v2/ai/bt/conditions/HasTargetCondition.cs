using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Condition that succeeds when a target exists in the blackboard.
    /// </summary>
    public class HasTargetCondition : BTLeafCondition
    {
        private readonly string _targetKey;

        public HasTargetCondition(string targetKey = "steering_target")
        {
            _targetKey = targetKey;
        }

        protected override bool CheckCondition()
        {
            return Context.Has(_targetKey);
        }
    }
}
