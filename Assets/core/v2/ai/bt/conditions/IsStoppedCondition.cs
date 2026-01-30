using UnityEngine;

namespace StarfireV2
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
            if (Context.Controller?.Rigid2D == null)
            {
                Debug.LogWarning("[IsStopped] No rigidbody, returning true");
                return true; // No rigidbody = effectively stopped
            }

            float speed = Context.Controller.Rigid2D.linearVelocity.magnitude;
            bool result = speed <= _threshold;

            return result;
        }
    }
}
