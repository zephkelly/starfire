using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for IsAtTargetCondition.
    /// </summary>
    [Serializable]
    public class IsAtTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the target position to check distance against.
        /// </summary>
        public string targetKey = "move_target";

        /// <summary>
        /// Distance threshold to consider "at target".
        /// </summary>
        public float threshold = 1f;
    }
}
