using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Starfire.Entity.Modules;

namespace Starfire.Entity
{
    public abstract class EntitySystemsBase : IEntitySystems
    {
        protected readonly EntityControllerBase Controller;
        protected readonly ModuleSlotCollection MultiSlots = new();

        /// <summary>
        /// Constructor for multi-slot system.
        /// </summary>
        protected EntitySystemsBase(EntityControllerBase controller, MultiSlotConfiguration[] configurations)
        {
            Controller = controller;

            if (configurations == null) return;

            var factories = GetSlotFactories();

            foreach (var config in configurations)
            {
                if (!config.isAvailable) continue;

                if (factories.TryGetValue(config.moduleType, out var factory))
                {
                    var slot = factory(controller);

                    // Set the slot ID so it can be used for hardpoint lookup
                    slot.SetSlotId(config.slotId);

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

        /// <summary>
        /// Override in derived class to provide factories for slot types.
        /// </summary>
        protected abstract Dictionary<ModuleTypeId, Func<EntityControllerBase, IModuleSlot>> GetSlotFactories();

        public void UpdateAll(float deltaTime)
        {
            MultiSlots.UpdateAll(deltaTime);
        }

        // === Slot methods ===

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
            return MultiSlots.GetModulesOfType<T>();
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
