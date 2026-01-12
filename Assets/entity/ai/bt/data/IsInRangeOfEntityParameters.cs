using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for IsInRangeOfEntityCondition.
    /// </summary>
    [Serializable]
    public class IsInRangeOfEntityParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the entity Transform to check distance to.
        /// </summary>
        public string entityKey = "target_entity";

        /// <summary>
        /// Minimum range to be considered "in range". Set to 0 for no minimum.
        /// </summary>
        public float minRange = 0f;

        /// <summary>
        /// Maximum range to be considered "in range".
        /// </summary>
        public float maxRange = 10f;
    }
}
