using System.Collections.Generic;
using System.Linq;

namespace Starfire.Entity.Modules
{
    /// <summary>
    /// Static registry providing hierarchy metadata for module types.
    /// Maps ModuleTypeId to Category/SubCategory and provides query methods.
    /// </summary>
    public static class ModuleHierarchyRegistry
    {
        private static readonly Dictionary<ModuleTypeId, (ModuleCategory Category, ModuleSubCategory SubCategory)> TypeHierarchy = new()
        {
            // Core > Structure
            { ModuleTypeId.Hull, (ModuleCategory.Core, ModuleSubCategory.Structure) },

            // Core > Defense
            { ModuleTypeId.Shield, (ModuleCategory.Core, ModuleSubCategory.Defense) },
            { ModuleTypeId.Deflector, (ModuleCategory.Core, ModuleSubCategory.Defense) },

            // Propulsion > Maneuvering
            { ModuleTypeId.ManeuveringThruster, (ModuleCategory.Propulsion, ModuleSubCategory.Maneuvering) },

            // Propulsion > Impulse
            { ModuleTypeId.ImpulseEngine, (ModuleCategory.Propulsion, ModuleSubCategory.Impulse) },

            // Propulsion > FTL
            { ModuleTypeId.WarpDrive, (ModuleCategory.Propulsion, ModuleSubCategory.FTL) },
            { ModuleTypeId.Hyperdrive, (ModuleCategory.Propulsion, ModuleSubCategory.FTL) },

            // Weapons > Offensive
            { ModuleTypeId.Laser, (ModuleCategory.Weapons, ModuleSubCategory.Offensive) },
            { ModuleTypeId.PlasmaCannon, (ModuleCategory.Weapons, ModuleSubCategory.Offensive) },
            { ModuleTypeId.MissileLauncher, (ModuleCategory.Weapons, ModuleSubCategory.Offensive) },

            // Weapons > Defensive
            { ModuleTypeId.PointDefenseTurret, (ModuleCategory.Weapons, ModuleSubCategory.Defensive) },

            // Systems > Sensors
            { ModuleTypeId.SensorArray, (ModuleCategory.Systems, ModuleSubCategory.Sensors) },

            // Systems > Communications
            { ModuleTypeId.Transponder, (ModuleCategory.Systems, ModuleSubCategory.Communications) },

            // Systems > Automation
            { ModuleTypeId.AICore, (ModuleCategory.Systems, ModuleSubCategory.Automation) },

            // Utility > Storage
            { ModuleTypeId.CargoBay, (ModuleCategory.Utility, ModuleSubCategory.Storage) },

            // Utility > Support
            { ModuleTypeId.LifeSupport, (ModuleCategory.Utility, ModuleSubCategory.Support) }
        };

        private static readonly Dictionary<ModuleTypeId, string> DisplayNames = new()
        {
            { ModuleTypeId.Hull, "Hull" },
            { ModuleTypeId.Shield, "Shield" },
            { ModuleTypeId.Deflector, "Deflector" },
            { ModuleTypeId.ManeuveringThruster, "Maneuvering Thruster" },
            { ModuleTypeId.ImpulseEngine, "Impulse Engine" },
            { ModuleTypeId.WarpDrive, "Warp Drive" },
            { ModuleTypeId.Hyperdrive, "Hyperdrive" },
            { ModuleTypeId.Laser, "Laser" },
            { ModuleTypeId.PlasmaCannon, "Plasma Cannon" },
            { ModuleTypeId.MissileLauncher, "Missile Launcher" },
            { ModuleTypeId.PointDefenseTurret, "Point Defense Turret" },
            { ModuleTypeId.SensorArray, "Sensor Array" },
            { ModuleTypeId.Transponder, "Transponder" },
            { ModuleTypeId.AICore, "AI Core" },
            { ModuleTypeId.CargoBay, "Cargo Bay" },
            { ModuleTypeId.LifeSupport, "Life Support" }
        };

        // Cached lookups for performance
        private static Dictionary<ModuleCategory, List<ModuleTypeId>> _typesByCategory;
        private static Dictionary<ModuleSubCategory, List<ModuleTypeId>> _typesBySubCategory;

        static ModuleHierarchyRegistry()
        {
            BuildCaches();
        }

        private static void BuildCaches()
        {
            _typesByCategory = new Dictionary<ModuleCategory, List<ModuleTypeId>>();
            _typesBySubCategory = new Dictionary<ModuleSubCategory, List<ModuleTypeId>>();

            foreach (var kvp in TypeHierarchy)
            {
                var typeId = kvp.Key;
                var (category, subCategory) = kvp.Value;

                if (!_typesByCategory.ContainsKey(category))
                    _typesByCategory[category] = new List<ModuleTypeId>();
                _typesByCategory[category].Add(typeId);

                if (!_typesBySubCategory.ContainsKey(subCategory))
                    _typesBySubCategory[subCategory] = new List<ModuleTypeId>();
                _typesBySubCategory[subCategory].Add(typeId);
            }
        }

        /// <summary>
        /// Gets the category for a module type.
        /// </summary>
        public static ModuleCategory GetCategory(ModuleTypeId typeId)
        {
            return TypeHierarchy.TryGetValue(typeId, out var hierarchy)
                ? hierarchy.Category
                : ModuleCategory.Utility;
        }

        /// <summary>
        /// Gets the subcategory for a module type.
        /// </summary>
        public static ModuleSubCategory GetSubCategory(ModuleTypeId typeId)
        {
            return TypeHierarchy.TryGetValue(typeId, out var hierarchy)
                ? hierarchy.SubCategory
                : ModuleSubCategory.Support;
        }

        /// <summary>
        /// Gets both category and subcategory for a module type.
        /// </summary>
        public static (ModuleCategory Category, ModuleSubCategory SubCategory) GetHierarchy(ModuleTypeId typeId)
        {
            return TypeHierarchy.TryGetValue(typeId, out var hierarchy)
                ? hierarchy
                : (ModuleCategory.Utility, ModuleSubCategory.Support);
        }

        /// <summary>
        /// Gets all module types in a category.
        /// </summary>
        public static IReadOnlyList<ModuleTypeId> GetTypesInCategory(ModuleCategory category)
        {
            return _typesByCategory.TryGetValue(category, out var types)
                ? types
                : new List<ModuleTypeId>();
        }

        /// <summary>
        /// Gets all module types in a subcategory.
        /// </summary>
        public static IReadOnlyList<ModuleTypeId> GetTypesInSubCategory(ModuleSubCategory subCategory)
        {
            return _typesBySubCategory.TryGetValue(subCategory, out var types)
                ? types
                : new List<ModuleTypeId>();
        }

        /// <summary>
        /// Gets the display name for a module type.
        /// </summary>
        public static string GetDisplayName(ModuleTypeId typeId)
        {
            return DisplayNames.TryGetValue(typeId, out var name)
                ? name
                : typeId.ToString();
        }

        /// <summary>
        /// Gets all subcategories within a category.
        /// </summary>
        public static IEnumerable<ModuleSubCategory> GetSubCategoriesInCategory(ModuleCategory category)
        {
            return _typesByCategory.TryGetValue(category, out var types)
                ? types.Select(t => GetSubCategory(t)).Distinct()
                : Enumerable.Empty<ModuleSubCategory>();
        }

        /// <summary>
        /// Checks if a module type belongs to a specific category.
        /// </summary>
        public static bool IsInCategory(ModuleTypeId typeId, ModuleCategory category)
        {
            return GetCategory(typeId) == category;
        }

        /// <summary>
        /// Checks if a module type belongs to a specific subcategory.
        /// </summary>
        public static bool IsInSubCategory(ModuleTypeId typeId, ModuleSubCategory subCategory)
        {
            return GetSubCategory(typeId) == subCategory;
        }

        /// <summary>
        /// Gets all registered module types.
        /// </summary>
        public static IEnumerable<ModuleTypeId> GetAllTypes()
        {
            return TypeHierarchy.Keys;
        }
    }
}
