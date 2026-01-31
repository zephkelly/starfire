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
        public IShipPropulsionModule Propulsion => Modules.GetModulesOfType<IShipPropulsionModule>().FirstOrDefault();
        public IShipRotationModule Rotation => Modules.GetModulesOfType<IShipRotationModule>().FirstOrDefault();

        // Category-based accessors
        public IEnumerable<IShipModule> DefenseModules => Modules.GetModulesOfType<IShipModule>().Where(m => m.Category == ShipModuleCategory.Defense);
        public IEnumerable<IShipOffensiveModule> OffenseModules => Modules.GetModulesOfType<IShipOffensiveModule>().Where(m => m.Category == ShipModuleCategory.Offense);
        public IEnumerable<IShipModule> PropulsionModules => Modules.GetModulesOfType<IShipModule>().Where(m => m.Category == ShipModuleCategory.Propulsion);
        public IEnumerable<IShipModule> SensorModules => Modules.GetModulesOfType<IShipModule>().Where(m => m.Category == ShipModuleCategory.Sensor);
        public IEnumerable<IShipModule> UtilityModules => Modules.GetModulesOfType<IShipModule>().Where(m => m.Category == ShipModuleCategory.Utility);
        
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

                if (config.hasOverrides && config.overrideData != null)
                {
                    slot.EquipFromData(config.overrideData);
                }
                else if (config.defaultModule != null)
                {
                    slot.EquipFromConfig(config.defaultModule);
                }
            }
        }

        public void UpdateModules(float deltaTime)
        {
            Modules.UpdateAll(deltaTime);
        }
    }
}