using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for SetTargetFromEntityAction.
    /// Copies an entity's position to the target key each tick.
    /// </summary>
    [Serializable]
    public class SetTargetFromEntityParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the Entity to track.
        /// </summary>
        public string entityKey = "steering_entity";

        /// <summary>
        /// Blackboard key to write the entity's position.
        /// </summary>
        public string targetKey = "steering_target";
    }
}
