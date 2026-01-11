using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for AimAtTargetAction.
    /// </summary>
    [Serializable]
    public class AimAtTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the target position to aim at.
        /// </summary>
        public string targetKey = "move_target";
    }
}
