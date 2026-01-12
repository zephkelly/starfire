using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for SetEntityAsTargetAction.
    /// </summary>
    [Serializable]
    public class SetEntityAsTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the entity Transform to target.
        /// </summary>
        public string entityKey = "target_entity";

        /// <summary>
        /// Blackboard key to write the target position to.
        /// </summary>
        public string targetKey = "move_target";
    }
}
