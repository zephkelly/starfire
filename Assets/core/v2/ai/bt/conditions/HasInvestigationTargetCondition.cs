using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Condition that succeeds when an investigation target exists in blackboard.
    /// </summary>
    public class HasInvestigationTargetCondition : BTLeafCondition
    {
        private readonly string _targetKey;

        public HasInvestigationTargetCondition(string targetKey = "investigation_target")
        {
            _targetKey = targetKey;
        }

        protected override bool CheckCondition()
        {
            bool hasTarget = Context.Has(_targetKey);
            return hasTarget;
        }
    }
}
