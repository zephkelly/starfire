using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for HasModuleTypeCondition.
    /// Checks if entity has a specific module type (from new hierarchy system) equipped.
    /// </summary>
    [Serializable]
    public class HasModuleTypeParameters : IBTNodeParameters
    {
        /// <summary>
        /// The specific module type to check for (e.g., ImpulseEngine, WarpDrive).
        /// </summary>
        public ShipModuleTypeId moduleType = ShipModuleTypeId.ImpluseEngine;
    }
}
