using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for CalculateSeekAction.
    /// Pure pursuit - no deceleration, always accelerates toward target.
    /// </summary>
    [Serializable]
    public class CalculateSeekParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for target position (Vector2).
        /// </summary>
        public string targetKey = "steering_target";

        /// <summary>
        /// Blackboard key to write the calculated steering force (Vector2).
        /// </summary>
        public string outputKey = "steering_force";
    }
}
