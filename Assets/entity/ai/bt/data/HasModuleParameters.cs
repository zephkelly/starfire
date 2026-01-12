using System;
using Starfire.Entity.Modules;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for HasModuleCondition.
    /// Checks if entity has a specific module type equipped.
    /// </summary>
    [Serializable]
    public class HasModuleParameters : IBTNodeParameters
    {
        /// <summary>
        /// The module type to check for.
        /// </summary>
        public ModuleSlotType moduleType = ModuleSlotType.Propulsion;
    }
}
