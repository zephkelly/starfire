using System;
using System.Collections.Generic;
using System.Linq;

namespace StarfireV2
{
    /// <summary>
    /// Centralized registry for module hierarchy metadata across all entity types.
    /// Uses integer-based storage internally to support different enum types per entity.
    /// </summary>
    public static class EntityModuleHierarchyRegistry
    {
        private static readonly Dictionary<(EntityType, int), int> _typeToCategory = new();
        private static readonly Dictionary<(EntityType, int), List<int>> _categoryToTypes = new();

        static EntityModuleHierarchyRegistry()
        {
            RegisterShipModules();
        }

        private static void Register(EntityType entityType, int typeId, int categoryId)
        {
            _typeToCategory[(entityType, typeId)] = categoryId;

            var key = (entityType, categoryId);
            if (!_categoryToTypes.ContainsKey(key))
                _categoryToTypes[key] = new List<int>();

            _categoryToTypes[key].Add(typeId);
        }

        private static void RegisterShipModules()
        {
            // Structure
            Register(EntityType.Ship, (int)ShipModuleTypeId.Hull, (int)ShipModuleCategory.Structure);

            // Defense (shields, deflectors, and defensive weapons)
            Register(EntityType.Ship, (int)ShipModuleTypeId.Shield, (int)ShipModuleCategory.Defense);
            Register(EntityType.Ship, (int)ShipModuleTypeId.Deflector, (int)ShipModuleCategory.Defense);
            Register(EntityType.Ship, (int)ShipModuleTypeId.FlakCannon, (int)ShipModuleCategory.Defense);
            Register(EntityType.Ship, (int)ShipModuleTypeId.FlareDispenser, (int)ShipModuleCategory.Defense);
            Register(EntityType.Ship, (int)ShipModuleTypeId.LaserPointDefense, (int)ShipModuleCategory.Defense);

            // Propulsion
            Register(EntityType.Ship, (int)ShipModuleTypeId.ManeuveringThrusters, (int)ShipModuleCategory.Propulsion);
            Register(EntityType.Ship, (int)ShipModuleTypeId.RotationalThrusters, (int)ShipModuleCategory.Propulsion);
            Register(EntityType.Ship, (int)ShipModuleTypeId.ImpluseEngine, (int)ShipModuleCategory.Propulsion);
            Register(EntityType.Ship, (int)ShipModuleTypeId.WarpDrive, (int)ShipModuleCategory.Propulsion);
            Register(EntityType.Ship, (int)ShipModuleTypeId.HyperDrive, (int)ShipModuleCategory.Propulsion);

            // Offense
            Register(EntityType.Ship, (int)ShipModuleTypeId.Laser, (int)ShipModuleCategory.Offense);
            Register(EntityType.Ship, (int)ShipModuleTypeId.PlasmaCannon, (int)ShipModuleCategory.Offense);
            Register(EntityType.Ship, (int)ShipModuleTypeId.MissileLauncher, (int)ShipModuleCategory.Offense);

            // Sensor
            Register(EntityType.Ship, (int)ShipModuleTypeId.SensorArray, (int)ShipModuleCategory.Sensor);

            // Comms
            Register(EntityType.Ship, (int)ShipModuleTypeId.Transponder, (int)ShipModuleCategory.Comms);

            // Utility
            Register(EntityType.Ship, (int)ShipModuleTypeId.CargoBay, (int)ShipModuleCategory.Utility);
            Register(EntityType.Ship, (int)ShipModuleTypeId.AICore, (int)ShipModuleCategory.Utility);
        }

        /// <summary>
        /// Gets the category for a module type (generic version).
        /// </summary>
        public static TCategory GetCategory<TCategory>(EntityType entityType, int typeId)
            where TCategory : struct, Enum
        {
            if (_typeToCategory.TryGetValue((entityType, typeId), out var categoryId))
                return (TCategory)(object)categoryId;

            throw new KeyNotFoundException($"No category found for entity {entityType} with type ID {typeId}");
        }

        /// <summary>
        /// Gets all type IDs in a category (returns as integers).
        /// </summary>
        public static IEnumerable<int> GetTypesInCategory(EntityType entityType, int categoryId)
        {
            return _categoryToTypes.TryGetValue((entityType, categoryId), out var types)
                ? types
                : Enumerable.Empty<int>();
        }

        /// <summary>
        /// Checks if a type ID exists for the given entity type.
        /// </summary>
        public static bool HasType(EntityType entityType, int typeId)
        {
            return _typeToCategory.ContainsKey((entityType, typeId));
        }

        #region Ship-Specific Convenience Methods

        /// <summary>
        /// Gets the category for a ship module type.
        /// </summary>
        public static ShipModuleCategory GetCategory(ShipModuleTypeId typeId)
        {
            return GetCategory<ShipModuleCategory>(EntityType.Ship, (int)typeId);
        }

        /// <summary>
        /// Gets all ship module types in a category.
        /// </summary>
        public static IEnumerable<ShipModuleTypeId> GetTypesInCategory(ShipModuleCategory category)
        {
            return GetTypesInCategory(EntityType.Ship, (int)category)
                .Select(id => (ShipModuleTypeId)id);
        }

        #endregion
    }
}
