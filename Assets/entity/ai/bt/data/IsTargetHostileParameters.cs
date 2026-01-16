using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for IsTargetHostileCondition.
    /// Checks if investigation target is hostile faction.
    /// </summary>
    [Serializable]
    public class IsTargetHostileParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the investigation target.
        /// </summary>
        public string targetKey = "investigation_target";
    }
}
