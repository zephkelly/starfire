using System;
using UnityEngine;

namespace Starfire.Entity.Modules
{
    /// <summary>
    /// Defines a limit on how many modules can be equipped within a scope.
    /// Used in ShipClassDefinition to enforce constraints like "max 2 offensive weapons".
    /// </summary>
    [Serializable]
    public class CategoryLimit
    {
        /// <summary>
        /// Defines what level of the hierarchy the limit applies to.
        /// </summary>
        public enum LimitScope
        {
            /// <summary>Limit applies to all modules in a category (e.g., all Weapons)</summary>
            Category,

            /// <summary>Limit applies to all modules in a subcategory (e.g., Offensive weapons only)</summary>
            SubCategory,

            /// <summary>Limit applies to a specific module type (e.g., Lasers only)</summary>
            ModuleType
        }

        [Tooltip("What level of hierarchy this limit applies to")]
        public LimitScope scope;

        [Tooltip("Category to limit (when scope is Category)")]
        public ModuleCategory category;

        [Tooltip("SubCategory to limit (when scope is SubCategory)")]
        public ModuleSubCategory subCategory;

        [Tooltip("ModuleType to limit (when scope is ModuleType)")]
        public ModuleTypeId moduleType;

        [Tooltip("Maximum number of modules/slots allowed")]
        [Min(0)]
        public int maxCount;

        /// <summary>
        /// Checks if this limit applies to the given module type.
        /// </summary>
        public bool AppliesToType(ModuleTypeId type)
        {
            return scope switch
            {
                LimitScope.Category => ModuleHierarchyRegistry.GetCategory(type) == category,
                LimitScope.SubCategory => ModuleHierarchyRegistry.GetSubCategory(type) == subCategory,
                LimitScope.ModuleType => type == moduleType,
                _ => false
            };
        }

        /// <summary>
        /// Gets a human-readable description of what this limit constrains.
        /// </summary>
        public string GetDescription()
        {
            string scopeName = scope switch
            {
                LimitScope.Category => category.ToString(),
                LimitScope.SubCategory => subCategory.ToString(),
                LimitScope.ModuleType => ModuleHierarchyRegistry.GetDisplayName(moduleType),
                _ => "Unknown"
            };
            return $"Max {maxCount} {scopeName}";
        }
    }
}
