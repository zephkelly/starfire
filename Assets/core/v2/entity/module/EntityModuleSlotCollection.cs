using System;
using System.Collections.Generic;
using System.Linq;

namespace StarfireV2
{
    /// <summary>
    /// Manages a collection of module slots with multi-slot support.
    /// Provides various query methods for accessing slots by ID, type, or category.
    /// </summary>
    /// <typeparam name="TCategory">The category enum type for this entity's modules.</typeparam>
    /// <typeparam name="TTypeId">The type ID enum for this entity's modules.</typeparam>
    public abstract class EntityModuleSlotCollection<TCategory, TTypeId>
        where TCategory : struct, Enum
        where TTypeId : struct, Enum
    {
        protected readonly Dictionary<string, IEntityModuleSlot> _slotsById = new();
        protected readonly Dictionary<string, TTypeId> _slotTypes = new();
        protected readonly Dictionary<TTypeId, List<IEntityModuleSlot>> _slotsByType = new();

        protected abstract IEnumerable<TTypeId> GetTypesInCategory(TCategory category);

        public void RegisterSlot(string slotId, TTypeId typeId, IEntityModuleSlot slot)
        {
            if (string.IsNullOrEmpty(slotId))
                throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

            if (slot == null)
                throw new ArgumentNullException(nameof(slot));

            if (_slotsById.ContainsKey(slotId))
            {
                UnityEngine.Debug.LogError($"[SlotCollection] DUPLICATE slotId '{slotId}'! " +
                    $"Previous slot will be OVERWRITTEN and orphaned. " +
                    $"Each module slot must have a unique slotId.");
            }

            _slotsById[slotId] = slot;
            _slotTypes[slotId] = typeId;

            if (!_slotsByType.ContainsKey(typeId))
                _slotsByType[typeId] = new List<IEntityModuleSlot>();

            _slotsByType[typeId].Add(slot);
        }

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

        public IEntityModuleSlot GetSlotById(string slotId)
        {
            return _slotsById.TryGetValue(slotId, out var slot) ? slot : null;
        }

        public TTypeId? GetSlotType(string slotId)
        {
            return _slotTypes.TryGetValue(slotId, out var typeId) ? typeId : null;
        }

        public IReadOnlyList<IEntityModuleSlot> GetSlotsByType(TTypeId typeId)
        {
            return _slotsByType.TryGetValue(typeId, out var slots)
                ? slots
                : Array.Empty<IEntityModuleSlot>();
        }

        public IEntityModuleSlot GetFirstSlotOfType(TTypeId typeId)
        {
            return GetSlotsByType(typeId).FirstOrDefault();
        }

        public IEnumerable<IEntityModuleSlot> GetSlotsByCategory(TCategory category)
        {
            foreach (var typeId in GetTypesInCategory(category))
            {
                foreach (var slot in GetSlotsByType(typeId))
                    yield return slot;
            }
        }

        public IEnumerable<T> GetModulesOfType<T>() where T : class, IEntityModule
        {
            foreach (var slot in _slotsById.Values)
            {
                if (slot.HasModule && slot.ModuleBase is T typedModule)
                    yield return typedModule;
            }
        }

        public T GetModuleBySlotId<T>(string slotId) where T : class, IEntityModule
        {
            var slot = GetSlotById(slotId);
            return slot?.ModuleBase as T;
        }

        public bool HasModuleOfType(TTypeId typeId)
        {
            return GetSlotsByType(typeId).Any(s => s.HasModule);
        }

        public int CountModulesInCategory(TCategory category)
        {
            return GetSlotsByCategory(category).Count(s => s.HasModule);
        }

        public int CountModulesOfType(TTypeId typeId)
        {
            return GetSlotsByType(typeId).Count(s => s.HasModule);
        }

        public int CountSlotsInCategory(TCategory category)
        {
            return GetSlotsByCategory(category).Count();
        }

        public IEnumerable<string> GetAllSlotIds()
        {
            return _slotsById.Keys;
        }

        public IEnumerable<IEntityModuleSlot> GetAllSlots()
        {
            return _slotsById.Values;
        }

        public int Count => _slotsById.Count;

        public void UpdateAll(float deltaTime)
        {
            foreach (var slot in _slotsById.Values)
            {
                slot.Update(deltaTime);
            }
        }
    }
}
