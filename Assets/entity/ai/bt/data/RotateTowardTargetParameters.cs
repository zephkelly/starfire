using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for RotateTowardTargetAction.
    /// Rotates entity to face the target position.
    /// </summary>
    [Serializable]
    public class RotateTowardTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for target position (Vector2).
        /// </summary>
        public string targetKey = "steering_target";
    }
}
