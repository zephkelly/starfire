using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for IsNearPositionCondition.
    /// Checks if entity is within threshold distance of target.
    /// </summary>
    [Serializable]
    public class IsNearPositionParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for target position (Vector2).
        /// </summary>
        public string targetKey = "steering_target";

        /// <summary>
        /// Distance threshold for "near" check.
        /// </summary>
        public float threshold = 1.0f;
    }
}
