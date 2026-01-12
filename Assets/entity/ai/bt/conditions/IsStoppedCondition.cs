using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when entity velocity is below threshold.
    /// </summary>
    public class IsStoppedCondition : BTLeafCondition
    {
        private readonly float _threshold;

        public IsStoppedCondition(float threshold = 0.5f)
        {
            _threshold = threshold;
        }

        protected override bool CheckCondition()
        {
            if (Context.Controller?.Rigidbody == null)
            {
                Debug.LogWarning("[IsStopped] No rigidbody, returning true");
                return true; // No rigidbody = effectively stopped
            }

            float speed = Context.Controller.Rigidbody.linearVelocity.magnitude;
            bool result = speed <= _threshold;

            return result;
        }
    }
}
