using System;
using System.Collections.Generic;
using System.Linq;

namespace Starfire.Entity.Modules
{
    /// <summary>
    /// Manages a collection of module slots with multi-slot support.
    /// Provides various query methods for accessing slots by ID, type, category, or subcategory.
    /// </summary>
    public class ModuleSlotCollection
    {
        private readonly Dictionary<string, IModuleSlot> _slotsById = new();
        private readonly Dictionary<string, ModuleTypeId> _slotTypes = new();
        private readonly Dictionary<ModuleTypeId, List<IModuleSlot>> _slotsByType = new();

        /// <summary>
        /// Registers a slot in the collection.
        /// </summary>
        public void RegisterSlot(string slotId, ModuleTypeId typeId, IModuleSlot slot)
        {
            if (string.IsNullOrEmpty(slotId))
                throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

            if (slot == null)
                throw new ArgumentNullException(nameof(slot));

            _slotsById[slotId] = slot;
            _slotTypes[slotId] = typeId;

            if (!_slotsByType.ContainsKey(typeId))
                _slotsByType[typeId] = new List<IModuleSlot>();

            _slotsByType[typeId].Add(slot);
        }

        /// <summary>
        /// Unregisters a slot from the collection.
        /// </summary>
        public bool UnregisterSlot(string slotId)
        {
            if (!_slotsById.TryGetValue(slotId, out var slot))
                return false;

            if (_slotTypes.TryGetValue(slotId, out var typeId) &&
                _slotsByType.TryGetValue(typeId, out var slots))
            {
                slots.Remove(slot);
            }

            _slotsById.Remove(slotId);
            _slotTypes.Remove(slotId);
            return true;
        }

        /// <summary>
        /// Gets a slot by its unique ID.
        /// </summary>
        public IModuleSlot GetSlotById(string slotId)
        {
            return _slotsById.TryGetValue(slotId, out var slot) ? slot : null;
        }

        /// <summary>
        /// Gets the module type ID for a slot.
        /// </summary>
        public ModuleTypeId? GetSlotType(string slotId)
        {
            return _slotTypes.TryGetValue(slotId, out var typeId) ? typeId : null;
        }

        /// <summary>
        /// Gets all slots of a specific module type.
        /// </summary>
        public IReadOnlyList<IModuleSlot> GetSlotsByType(ModuleTypeId typeId)
        {
            return _slotsByType.TryGetValue(typeId, out var slots)
                ? slots
                : Array.Empty<IModuleSlot>();
        }

        /// <summary>
        /// Gets the first slot of a specific module type, or null if none exist.
        /// </summary>
        public IModuleSlot GetFirstSlotOfType(ModuleTypeId typeId)
        {
            return GetSlotsByType(typeId).FirstOrDefault();
        }

        /// <summary>
        /// Gets all slots belonging to a category.
        /// </summary>
        public IEnumerable<IModuleSlot> GetSlotsByCategory(ModuleCategory category)
        {
            foreach (var typeId in ModuleHierarchyRegistry.GetTypesInCategory(category))
            {
                foreach (var slot in GetSlotsByType(typeId))
                    yield return slot;
            }
        }

        /// <summary>
        /// Gets all slots belonging to a subcategory.
        /// </summary>
        public IEnumerable<IModuleSlot> GetSlotsBySubCategory(ModuleSubCategory subCategory)
        {
            foreach (var typeId in ModuleHierarchyRegistry.GetTypesInSubCategory(subCategory))
            {
                foreach (var slot in GetSlotsByType(typeId))
                    yield return slot;
            }
        }

        /// <summary>
        /// Gets all modules of a specific interface type.
        /// </summary>
        public IEnumerable<T> GetModulesOfType<T>() where T : class, IEntityModule
        {
            foreach (var slot in _slotsById.Values)
            {
                if (slot.HasModule && slot.ModuleBase is T typedModule)
                    yield return typedModule;
            }
        }

        /// <summary>
        /// Gets a specific module by slot ID with type casting.
        /// </summary>
        public T GetModuleBySlotId<T>(string slotId) where T : class, IEntityModule
        {
            var slot = GetSlotById(slotId);
            return slot?.ModuleBase as T;
        }

        /// <summary>
        /// Checks if any slot of the given type has a module equipped.
        /// </summary>
        public bool HasModuleOfType(ModuleTypeId typeId)
        {
            return GetSlotsByType(typeId).Any(s => s.HasModule);
        }

        /// <summary>
        /// Counts equipped modules in a category.
        /// </summary>
        public int CountModulesInCategory(ModuleCategory category)
        {
            return GetSlotsByCategory(category).Count(s => s.HasModule);
        }

        /// <summary>
        /// Counts equipped modules in a subcategory.
        /// </summary>
        public int CountModulesInSubCategory(ModuleSubCategory subCategory)
        {
            return GetSlotsBySubCategory(subCategory).Count(s => s.HasModule);
        }

        /// <summary>
        /// Counts equipped modules of a specific type.
        /// </summary>
        public int CountModulesOfType(ModuleTypeId typeId)
        {
            return GetSlotsByType(typeId).Count(s => s.HasModule);
        }

        /// <summary>
        /// Counts total slots in a category (equipped or not).
        /// </summary>
        public int CountSlotsInCategory(ModuleCategory category)
        {
            return GetSlotsByCategory(category).Count();
        }

        /// <summary>
        /// Counts total slots in a subcategory (equipped or not).
        /// </summary>
        public int CountSlotsInSubCategory(ModuleSubCategory subCategory)
        {
            return GetSlotsBySubCategory(subCategory).Count();
        }

        /// <summary>
        /// Gets all registered slot IDs.
        /// </summary>
        public IEnumerable<string> GetAllSlotIds()
        {
            return _slotsById.Keys;
        }

        /// <summary>
        /// Gets all registered slots.
        /// </summary>
        public IEnumerable<IModuleSlot> GetAllSlots()
        {
            return _slotsById.Values;
        }

        /// <summary>
        /// Gets total number of slots.
        /// </summary>
        public int Count => _slotsById.Count;

        /// <summary>
        /// Updates all modules in all slots.
        /// </summary>
        public void UpdateAll(float deltaTime)
        {
            foreach (var slot in _slotsById.Values)
            {
                slot.Update(deltaTime);
            }
        }
    }
}
