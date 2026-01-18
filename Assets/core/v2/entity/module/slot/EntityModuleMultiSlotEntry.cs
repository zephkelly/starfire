using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Defines a module slot configuration for the multi-slot system.
    /// Each entry represents a single slot that can hold one module.
    /// Multiple entries of the same ModuleTypeId create multiple slots.
    /// </summary>
    [Serializable]
    public class EntityModuleMultiSlotEntry<TModuleTypeId, TCategory>
        where TModuleTypeId : struct, Enum
        where TCategory : struct, Enum
    {
        [Tooltip("The entity type this slot belongs to")]
        public EntityType entityType;

        [Tooltip("Unique identifier for this slot (e.g., 'primary_laser', 'port_thruster')")]
        public string slotId;

        [Tooltip("The specific module type this slot accepts")]
        public TModuleTypeId moduleType;

        [Tooltip("Whether a module must be installed in this slot")]
        public bool isRequired;

        [Tooltip("Default module configuration to equip")]
        public ScriptableObject defaultModule;

        [Tooltip("Optional display name override for the inspector")]
        public string displayNameOverride;

        public string DisplayName => string.IsNullOrEmpty(displayNameOverride)
            ? slotId
            : displayNameOverride;

        public TCategory Category =>
            EntityModuleHierarchyRegistry.GetCategory<TCategory>(entityType, Convert.ToInt32(moduleType));
    }
}
