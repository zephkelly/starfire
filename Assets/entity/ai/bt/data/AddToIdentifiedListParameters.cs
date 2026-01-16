using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for AddToIdentifiedListAction.
    /// Adds an entity to the identified list to avoid re-investigating.
    /// </summary>
    [Serializable]
    public class AddToIdentifiedListParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the entity to add.
        /// </summary>
        public string targetKey = "investigation_target";

        /// <summary>
        /// Blackboard key for the identified entities list.
        /// </summary>
        public string listKey = "identified_entities";
    }
}
