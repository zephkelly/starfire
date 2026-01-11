using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for MoveToAction.
    /// </summary>
    [Serializable]
    public class MoveToParameters : IBTNodeParameters
    {
        /// <summary>
        /// Distance at which the agent is considered to have arrived.
        /// </summary>
        public float arrivalThreshold = 1f;

        /// <summary>
        /// Multiplier for the slowing radius (slowingRadius = arrivalThreshold * this value).
        /// </summary>
        public float slowingMultiplier = 5f;

        /// <summary>
        /// Blackboard key for the target position (Vector2).
        /// </summary>
        public string targetKey = "move_target";
    }
}
