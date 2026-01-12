using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for CalculateFleeAction.
    /// Accelerates away from target position.
    /// </summary>
    [Serializable]
    public class CalculateFleeParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for position to flee from (Vector2).
        /// </summary>
        public string targetKey = "steering_target";

        /// <summary>
        /// Blackboard key to write the calculated steering force (Vector2).
        /// </summary>
        public string outputKey = "steering_force";
    }
}
