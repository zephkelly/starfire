using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for ClearInvestigationTargetAction.
    /// Clears the current investigation target from blackboard.
    /// </summary>
    [Serializable]
    public class ClearInvestigationTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to clear.
        /// </summary>
        public string targetKey = "investigation_target";
    }
}
