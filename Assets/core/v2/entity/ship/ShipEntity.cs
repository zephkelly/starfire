using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarfireV2
{
    public class ShipEntity : IEntity
    {
        public EntityType EntityType { get; }
        public int Id { get; }

        public ShipModuleSlotCollection Modules { get; } = new();

        // Typed accessors
        public IShipHullModule Hull => Modules.GetModulesOfType<IShipHullModule>().FirstOrDefault();

        // Category-based accessors
        public IEnumerable<IShipModule> DefenseModules => GetModulesInCategory(ShipModuleCategory.Defense);
        public IEnumerable<IShipModule> OffenseModules => GetModulesInCategory(ShipModuleCategory.Offense);
        public IEnumerable<IShipModule> PropulsionModules => GetModulesInCategory(ShipModuleCategory.Propulsion);
        public IEnumerable<IShipModule> SensorModules => GetModulesInCategory(ShipModuleCategory.Sensor);
        public IEnumerable<IShipModule> UtilityModules => GetModulesInCategory(ShipModuleCategory.Utility);

        public ShipEntity(EntityType type, int id)
        {
            EntityType = type;
            Id = id;
        }

        public void InitializeModules(IEntityController controller, ModuleSlotConfiguration[] configurations)
        {
            if (configurations == null) return;

            foreach (var config in configurations)
            {
                if (!config.isAvailable) continue;

                var slot = ShipModuleSlotFactory.CreateSlot(config.typeId, controller);
                if (slot == null) continue;

                slot.SetSlotId(config.slotId);
                Modules.RegisterSlot(config.slotId, config.typeId, slot);

                if (config.defaultModule != null)
                {
                    slot.EquipFromConfig(config.defaultModule);
                }
            }
        }

        public void UpdateModules(float deltaTime)
        {
            Modules.UpdateAll(deltaTime);
        }

        private IEnumerable<IShipModule> GetModulesInCategory(ShipModuleCategory category)
        {
            return Modules.GetSlotsByCategory(category)
                .Where(s => s.HasModule)
                .Select(s => s.ModuleBase)
                .OfType<IShipModule>();
        }
    }
}