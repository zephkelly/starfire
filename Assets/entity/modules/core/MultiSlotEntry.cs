using System;
using UnityEngine;

namespace Starfire.Entity.Modules
{
    /// <summary>
    /// Defines a module slot configuration for the multi-slot system.
    /// Each entry represents a single slot that can hold one module.
    /// Multiple entries of the same ModuleTypeId create multiple slots.
    /// </summary>
    [Serializable]
    public class MultiSlotEntry
    {
        [Tooltip("Unique identifier for this slot (e.g., 'primary_laser', 'port_thruster')")]
        public string slotId;

        [Tooltip("The specific module type this slot accepts")]
        public ModuleTypeId moduleType;

        [Tooltip("Whether a module must be installed in this slot")]
        public bool isRequired;

        [Tooltip("Default module configuration to equip")]
        public ScriptableObject defaultModule;

        [Tooltip("Optional display name override for the inspector")]
        public string displayNameOverride;

        /// <summary>
        /// Gets the display name, using override if set, otherwise the slot ID.
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(displayNameOverride)
            ? slotId
            : displayNameOverride;

        /// <summary>
        /// Gets the category this slot belongs to.
        /// </summary>
        public ModuleCategory Category => ModuleHierarchyRegistry.GetCategory(moduleType);

        /// <summary>
        /// Gets the subcategory this slot belongs to.
        /// </summary>
        public ModuleSubCategory SubCategory => ModuleHierarchyRegistry.GetSubCategory(moduleType);
    }
}
