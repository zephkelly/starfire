using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Entity.Modules;

namespace Starfire.Entity
{
    public abstract class EntitySystemsBase : IEntitySystems
    {
        protected readonly Dictionary<ModuleSlotType, IModuleSlot> Slots = new();
        protected readonly HashSet<ModuleSlotType> AvailableSlots = new();
        protected readonly EntityControllerBase Controller;

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

        protected abstract Dictionary<ModuleSlotType, Func<EntityControllerBase, IModuleSlot>> GetSlotFactories();

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
            foreach (var slot in Slots.Values)
            {
                slot.Update(deltaTime);
            }
        }

        protected ModuleSlot<T> GetTypedSlot<T>(ModuleSlotType type) where T : class, IEntityModule
        {
            if (Slots.TryGetValue(type, out var slot))
            {
                return slot as ModuleSlot<T>;
            }
            return null;
        }
    }
}
