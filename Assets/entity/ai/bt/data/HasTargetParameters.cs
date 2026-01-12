using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for HasTargetCondition.
    /// </summary>
    [Serializable]
    public class HasTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to check for existence.
        /// </summary>
        public string targetKey = "target_entity";
    }
}
