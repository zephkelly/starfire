using System;
using Starfire.Entity.Modules;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for HasModuleCategoryCondition.
    /// Checks if entity has any module in a specific category.
    /// </summary>
    [Serializable]
    public class HasModuleCategoryParameters : IBTNodeParameters
    {
        /// <summary>
        /// The category to check (e.g., Propulsion, Weapons).
        /// </summary>
        public ModuleCategory category = ModuleCategory.Propulsion;

        /// <summary>
        /// Minimum number of equipped modules required for condition to pass.
        /// </summary>
        public int minimumCount = 1;
    }
}
