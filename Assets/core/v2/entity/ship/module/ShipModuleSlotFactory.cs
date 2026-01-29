using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    public static class ShipModuleSlotFactory
    {
        private static readonly Dictionary<ShipModuleTypeId, Func<IEntityController, IEntityModuleSlot>> Factories = new()
        {
            // Structure
            { ShipModuleTypeId.Hull, c => new ShipModuleSlot<IShipHullModule>(c) },

            // Defense
            { ShipModuleTypeId.Shield, c => new ShipModuleSlot<IShipModule>(c) },
            { ShipModuleTypeId.Deflector, c => new ShipModuleSlot<IShipModule>(c) },

            // Propulsion - Maneuvering
            { ShipModuleTypeId.ManeuveringThrusters, c => new ShipModuleSlot<IShipModule>(c) },
            { ShipModuleTypeId.RotationalThrusters, c => new ShipModuleSlot<IShipModule>(c) },

            // Propulsion - Impulse
            { ShipModuleTypeId.ImpluseEngine, c => new ShipModuleSlot<IShipModule>(c) },

            // Propulsion - FTL
            { ShipModuleTypeId.WarpDrive, c => new ShipModuleSlot<IShipModule>(c) },
            { ShipModuleTypeId.HyperDrive, c => new ShipModuleSlot<IShipModule>(c) },

            // Weapons - Offensive
            { ShipModuleTypeId.Weapon, c => new ShipModuleSlot<IWeaponModule>(c) },

            // Weapons - Point Defense
            { ShipModuleTypeId.PointDefense, c => new ShipModuleSlot<IDefensiveWeaponModule>(c) },

            // Sensors
            { ShipModuleTypeId.SensorArray, c => new ShipModuleSlot<ISensorModule>(c) },

            // Comms
            { ShipModuleTypeId.Transponder, c => new ShipModuleSlot<ITransponderModule>(c) },

            // Utility
            { ShipModuleTypeId.CargoBay, c => new ShipModuleSlot<IShipModule>(c) },
            { ShipModuleTypeId.AICore, c => new ShipModuleSlot<IShipModule>(c) },
        };

        public static IEntityModuleSlot CreateSlot(ShipModuleTypeId typeId, IEntityController controller)
        {
            if (Factories.TryGetValue(typeId, out var factory))
            {
                return factory(controller);
            }

            Debug.LogWarning($"No slot factory registered for module type: {typeId}");
            return null;
        }

        public static bool HasFactory(ShipModuleTypeId typeId)
        {
            return Factories.ContainsKey(typeId);
        }
    }
}
