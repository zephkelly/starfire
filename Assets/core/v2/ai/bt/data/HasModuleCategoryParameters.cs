using System;

namespace StarfireV2
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
        public ShipModuleCategory category = ShipModuleCategory.Propulsion;

        /// <summary>
        /// Minimum number of equipped modules required for condition to pass.
        /// </summary>
        public int minimumCount = 1;
    }
}
