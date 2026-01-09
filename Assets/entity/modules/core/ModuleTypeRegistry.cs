using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Entity.Modules.Hull;
using Starfire.Entity.Modules.Shield;
using Starfire.Entity.Modules.Deflector;
using Starfire.Entity.Modules.Propulsion;
using Starfire.Entity.Modules.Rotation;
using Starfire.Entity.Modules.WarpDrive;
using Starfire.Entity.Modules.Hyperdrive;
using Starfire.Entity.Modules.Weapon;
using Starfire.Entity.Modules.Sensor;
using Starfire.Entity.Modules.Transponder;
using Starfire.Entity.Modules.CargoBay;
using Starfire.Entity.Modules.AICore;
using Starfire.Entity.Modules.LifeSupport;

namespace Starfire.Entity.Modules
{
    public static class ModuleTypeRegistry
    {
        private static readonly Dictionary<ModuleSlotType, Type> SlotToConfigType = new()
        {
            { ModuleSlotType.Hull, typeof(HullModuleConfig) },
            { ModuleSlotType.Shield, typeof(ShieldModuleConfig) },
            { ModuleSlotType.Deflector, typeof(DeflectorModuleConfig) },
            { ModuleSlotType.Propulsion, typeof(PropulsionModuleConfig) },
            { ModuleSlotType.Rotation, typeof(RotationModuleConfig) },
            { ModuleSlotType.WarpDrive, typeof(WarpDriveModuleConfig) },
            { ModuleSlotType.Hyperdrive, typeof(HyperdriveModuleConfig) },
            { ModuleSlotType.Weapon, typeof(WeaponModuleConfig) },
            { ModuleSlotType.Sensor, typeof(SensorModuleConfig) },
            { ModuleSlotType.Transponder, typeof(TransponderModuleConfig) },
            { ModuleSlotType.CargoBay, typeof(CargoBayModuleConfig) },
            { ModuleSlotType.AICore, typeof(AICoreModuleConfig) },
            { ModuleSlotType.LifeSupport, typeof(LifeSupportModuleConfig) },
        };

        public static Type GetConfigTypeForSlot(ModuleSlotType slotType)
        {
            return SlotToConfigType.TryGetValue(slotType, out var type) ? type : null;
        }

        public static bool IsValidConfigForSlot(ModuleSlotType slotType, ScriptableObject config)
        {
            if (config == null) return true;
            var expectedType = GetConfigTypeForSlot(slotType);
            return expectedType != null && expectedType.IsInstanceOfType(config);
        }

        public static string GetDisplayName(ModuleSlotType slotType)
        {
            return slotType switch
            {
                ModuleSlotType.WarpDrive => "Warp Drive",
                ModuleSlotType.AICore => "AI Core",
                ModuleSlotType.CargoBay => "Cargo Bay",
                ModuleSlotType.LifeSupport => "Life Support",
                _ => slotType.ToString()
            };
        }
    }
}
