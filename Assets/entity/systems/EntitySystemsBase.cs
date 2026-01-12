using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Starfire.Entity.Modules;

namespace Starfire.Entity
{
    public abstract class EntitySystemsBase : IEntitySystems
    {
        // Legacy single-slot storage (backwards compatible)
        protected readonly Dictionary<ModuleSlotType, IModuleSlot> Slots = new();
        protected readonly HashSet<ModuleSlotType> AvailableSlots = new();
        protected readonly EntityControllerBase Controller;

        // New multi-slot storage
        protected readonly ModuleSlotCollection MultiSlots = new();

        /// <summary>
        /// Constructor for legacy single-slot system (backwards compatible).
        /// </summary>
        protected EntitySystemsBase(EntityControllerBase controller, SlotConfiguration[] configurations)
        {
            Controller = controller;
            var factories = GetSlotFactories();

            foreach (var config in configurations)
            {
                if (!config.isAvailable) continue;

                AvailableSlots.Add(config.slotType);

                if (factories.TryGetValue(config.slotType, out var factory))
                {
                    var slot = factory(controller);
                    Slots[config.slotType] = slot;

                    if (config.defaultModule != null)
                    {
                        slot.EquipFromConfig(config.defaultModule);
                    }
                }
                else
                {
                    Debug.LogWarning($"No factory registered for slot type: {config.slotType}");
                }
            }
        }

        /// <summary>
        /// Constructor with multi-slot support.
        /// </summary>
        protected EntitySystemsBase(
            EntityControllerBase controller,
            SlotConfiguration[] legacyConfigurations,
            MultiSlotConfiguration[] multiConfigurations)
            : this(controller, legacyConfigurations)
        {
            if (multiConfigurations == null) return;

            var multiFactories = GetMultiSlotFactories();

            foreach (var config in multiConfigurations)
            {
                if (!config.isAvailable) continue;

                if (multiFactories.TryGetValue(config.moduleType, out var factory))
                {
                    var slot = factory(controller);
                    MultiSlots.RegisterSlot(config.slotId, config.moduleType, slot);

                    if (config.defaultModule != null)
                    {
                        slot.EquipFromConfig(config.defaultModule);
                    }
                }
                else
                {
                    Debug.LogWarning($"No factory registered for module type: {config.moduleType}");
                }
            }
        }

        protected abstract Dictionary<ModuleSlotType, Func<EntityControllerBase, IModuleSlot>> GetSlotFactories();

        /// <summary>
        /// Override in derived class to provide factories for multi-slot types.
        /// </summary>
        protected virtual Dictionary<ModuleTypeId, Func<EntityControllerBase, IModuleSlot>> GetMultiSlotFactories()
        {
            return new Dictionary<ModuleTypeId, Func<EntityControllerBase, IModuleSlot>>();
        }

        // === Legacy single-slot methods ===

        public bool IsSlotAvailable(ModuleSlotType type) => AvailableSlots.Contains(type);

        public bool HasModule(ModuleSlotType type)
        {
            return Slots.TryGetValue(type, out var slot) && slot.HasModule;
        }

        public IModuleSlot GetSlot(ModuleSlotType type)
        {
            Slots.TryGetValue(type, out var slot);
            return slot;
        }

        public T GetModule<T>(ModuleSlotType type) where T : class, IEntityModule
        {
            if (Slots.TryGetValue(type, out var slot))
            {
                return slot.ModuleBase as T;
            }
            return null;
        }

        public void UpdateAll(float deltaTime)
        {
            // Update legacy slots
            foreach (var slot in Slots.Values)
            {
                slot.Update(deltaTime);
            }

            // Update multi-slots
            MultiSlots.UpdateAll(deltaTime);
        }

        protected ModuleSlot<T> GetTypedSlot<T>(ModuleSlotType type) where T : class, IEntityModule
        {
            if (Slots.TryGetValue(type, out var slot))
            {
                return slot as ModuleSlot<T>;
            }
            return null;
        }

        // === Multi-slot methods ===

        public IModuleSlot GetSlotById(string slotId)
        {
            return MultiSlots.GetSlotById(slotId);
        }

        public IReadOnlyList<IModuleSlot> GetSlotsByType(ModuleTypeId typeId)
        {
            return MultiSlots.GetSlotsByType(typeId);
        }

        public IEnumerable<IModuleSlot> GetSlotsByCategory(ModuleCategory category)
        {
            return MultiSlots.GetSlotsByCategory(category);
        }

        public IEnumerable<IModuleSlot> GetSlotsBySubCategory(ModuleSubCategory subCategory)
        {
            return MultiSlots.GetSlotsBySubCategory(subCategory);
        }

        public IEnumerable<T> GetAllModulesOfType<T>() where T : class, IEntityModule
        {
            // Include both legacy and multi-slot modules
            foreach (var slot in Slots.Values)
            {
                if (slot.HasModule && slot.ModuleBase is T typedModule)
                    yield return typedModule;
            }

            foreach (var module in MultiSlots.GetModulesOfType<T>())
            {
                yield return module;
            }
        }

        public bool HasModuleOfType(ModuleTypeId typeId)
        {
            return MultiSlots.HasModuleOfType(typeId);
        }

        public int CountModulesInCategory(ModuleCategory category)
        {
            return MultiSlots.CountModulesInCategory(category);
        }

        public int CountModulesInSubCategory(ModuleSubCategory subCategory)
        {
            return MultiSlots.CountModulesInSubCategory(subCategory);
        }

        /// <summary>
        /// Gets a module by slot ID with type casting.
        /// </summary>
        public T GetModuleBySlotId<T>(string slotId) where T : class, IEntityModule
        {
            return MultiSlots.GetModuleBySlotId<T>(slotId);
        }
    }
}
