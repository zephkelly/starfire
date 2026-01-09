using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Entity.Modules;
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

namespace Starfire.Entity
{
    public class ShipSystems
    {
        private readonly Dictionary<ModuleSlotType, object> _slots = new();
        private readonly HashSet<ModuleSlotType> _availableSlots = new();
        private readonly EntityController _controller;

        public ModuleSlot<IHullModule> Hull => GetSlot<IHullModule>(ModuleSlotType.Hull);
        public ModuleSlot<IShieldModule> Shield => GetSlot<IShieldModule>(ModuleSlotType.Shield);
        public ModuleSlot<IDeflectorModule> Deflector => GetSlot<IDeflectorModule>(ModuleSlotType.Deflector);
        public ModuleSlot<IPropulsionModule> Propulsion => GetSlot<IPropulsionModule>(ModuleSlotType.Propulsion);
        public ModuleSlot<IRotationModule> Rotation => GetSlot<IRotationModule>(ModuleSlotType.Rotation);
        public ModuleSlot<IWarpDriveModule> WarpDrive => GetSlot<IWarpDriveModule>(ModuleSlotType.WarpDrive);
        public ModuleSlot<IHyperdriveModule> Hyperdrive => GetSlot<IHyperdriveModule>(ModuleSlotType.Hyperdrive);
        public ModuleSlot<IWeaponModule> Weapon => GetSlot<IWeaponModule>(ModuleSlotType.Weapon);
        public ModuleSlot<ISensorModule> Sensor => GetSlot<ISensorModule>(ModuleSlotType.Sensor);
        public ModuleSlot<ITransponderModule> Transponder => GetSlot<ITransponderModule>(ModuleSlotType.Transponder);
        public ModuleSlot<ICargoBayModule> CargoBay => GetSlot<ICargoBayModule>(ModuleSlotType.CargoBay);
        public ModuleSlot<IAICoreModule> AICore => GetSlot<IAICoreModule>(ModuleSlotType.AICore);
        public ModuleSlot<ILifeSupportModule> LifeSupport => GetSlot<ILifeSupportModule>(ModuleSlotType.LifeSupport);

        public ShipSystems(EntityController controller, SlotConfiguration[] configurations)
        {
            _controller = controller;

            foreach (var config in configurations)
            {
                if (!config.isAvailable) continue;

                _availableSlots.Add(config.slotType);
                var slot = CreateSlotForType(config.slotType);
                _slots[config.slotType] = slot;

                if (config.defaultModule != null)
                {
                    EquipDefaultModule(config.slotType, config.defaultModule);
                }
            }
        }

        public bool IsSlotAvailable(ModuleSlotType type) => _availableSlots.Contains(type);

        public bool HasModule(ModuleSlotType type)
        {
            if (!_slots.TryGetValue(type, out var slot)) return false;

            return type switch
            {
                ModuleSlotType.Hull => ((ModuleSlot<IHullModule>)slot).HasModule,
                ModuleSlotType.Shield => ((ModuleSlot<IShieldModule>)slot).HasModule,
                ModuleSlotType.Deflector => ((ModuleSlot<IDeflectorModule>)slot).HasModule,
                ModuleSlotType.Propulsion => ((ModuleSlot<IPropulsionModule>)slot).HasModule,
                ModuleSlotType.Rotation => ((ModuleSlot<IRotationModule>)slot).HasModule,
                ModuleSlotType.WarpDrive => ((ModuleSlot<IWarpDriveModule>)slot).HasModule,
                ModuleSlotType.Hyperdrive => ((ModuleSlot<IHyperdriveModule>)slot).HasModule,
                ModuleSlotType.Weapon => ((ModuleSlot<IWeaponModule>)slot).HasModule,
                ModuleSlotType.Sensor => ((ModuleSlot<ISensorModule>)slot).HasModule,
                ModuleSlotType.Transponder => ((ModuleSlot<ITransponderModule>)slot).HasModule,
                ModuleSlotType.CargoBay => ((ModuleSlot<ICargoBayModule>)slot).HasModule,
                ModuleSlotType.AICore => ((ModuleSlot<IAICoreModule>)slot).HasModule,
                ModuleSlotType.LifeSupport => ((ModuleSlot<ILifeSupportModule>)slot).HasModule,
                _ => false
            };
        }

        public void UpdateAll(float deltaTime)
        {
            foreach (var kvp in _slots)
            {
                UpdateSlot(kvp.Key, kvp.Value, deltaTime);
            }
        }

        private ModuleSlot<T> GetSlot<T>(ModuleSlotType type) where T : class, IShipModule
        {
            if (_slots.TryGetValue(type, out var slot))
            {
                return slot as ModuleSlot<T>;
            }
            return null;
        }

        private object CreateSlotForType(ModuleSlotType type)
        {
            return type switch
            {
                ModuleSlotType.Hull => new ModuleSlot<IHullModule>(_controller),
                ModuleSlotType.Shield => new ModuleSlot<IShieldModule>(_controller),
                ModuleSlotType.Deflector => new ModuleSlot<IDeflectorModule>(_controller),
                ModuleSlotType.Propulsion => new ModuleSlot<IPropulsionModule>(_controller),
                ModuleSlotType.Rotation => new ModuleSlot<IRotationModule>(_controller),
                ModuleSlotType.WarpDrive => new ModuleSlot<IWarpDriveModule>(_controller),
                ModuleSlotType.Hyperdrive => new ModuleSlot<IHyperdriveModule>(_controller),
                ModuleSlotType.Weapon => new ModuleSlot<IWeaponModule>(_controller),
                ModuleSlotType.Sensor => new ModuleSlot<ISensorModule>(_controller),
                ModuleSlotType.Transponder => new ModuleSlot<ITransponderModule>(_controller),
                ModuleSlotType.CargoBay => new ModuleSlot<ICargoBayModule>(_controller),
                ModuleSlotType.AICore => new ModuleSlot<IAICoreModule>(_controller),
                ModuleSlotType.LifeSupport => new ModuleSlot<ILifeSupportModule>(_controller),
                _ => throw new ArgumentException($"Unknown slot type: {type}")
            };
        }

        private void EquipDefaultModule(ModuleSlotType type, ScriptableObject config)
        {
            switch (type)
            {
                case ModuleSlotType.Hull when config is HullModuleConfig hullConfig:
                    Hull?.Equip(hullConfig.CreateModule());
                    break;
                case ModuleSlotType.Shield when config is ShieldModuleConfig shieldConfig:
                    Shield?.Equip(shieldConfig.CreateModule());
                    break;
                case ModuleSlotType.Deflector when config is DeflectorModuleConfig deflectorConfig:
                    Deflector?.Equip(deflectorConfig.CreateModule());
                    break;
                case ModuleSlotType.Propulsion when config is PropulsionModuleConfig propulsionConfig:
                    Propulsion?.Equip(propulsionConfig.CreateModule());
                    break;
                case ModuleSlotType.Rotation when config is RotationModuleConfig rotationConfig:
                    Rotation?.Equip(rotationConfig.CreateModule());
                    break;
                case ModuleSlotType.WarpDrive when config is WarpDriveModuleConfig warpConfig:
                    WarpDrive?.Equip(warpConfig.CreateModule());
                    break;
                case ModuleSlotType.Hyperdrive when config is HyperdriveModuleConfig hyperdriveConfig:
                    Hyperdrive?.Equip(hyperdriveConfig.CreateModule());
                    break;
                case ModuleSlotType.Weapon when config is WeaponModuleConfig weaponConfig:
                    Weapon?.Equip(weaponConfig.CreateModule());
                    break;
                case ModuleSlotType.Sensor when config is SensorModuleConfig sensorConfig:
                    Sensor?.Equip(sensorConfig.CreateModule());
                    break;
                case ModuleSlotType.Transponder when config is TransponderModuleConfig transponderConfig:
                    Transponder?.Equip(transponderConfig.CreateModule());
                    break;
                case ModuleSlotType.CargoBay when config is CargoBayModuleConfig cargoConfig:
                    CargoBay?.Equip(cargoConfig.CreateModule());
                    break;
                case ModuleSlotType.AICore when config is AICoreModuleConfig aiConfig:
                    AICore?.Equip(aiConfig.CreateModule());
                    break;
                case ModuleSlotType.LifeSupport when config is LifeSupportModuleConfig lifeSupportConfig:
                    LifeSupport?.Equip(lifeSupportConfig.CreateModule());
                    break;
                default:
                    Debug.LogWarning($"Could not equip default module for slot type {type}. Config type mismatch.");
                    break;
            }
        }

        private void UpdateSlot(ModuleSlotType type, object slot, float deltaTime)
        {
            switch (type)
            {
                case ModuleSlotType.Hull:
                    ((ModuleSlot<IHullModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.Shield:
                    ((ModuleSlot<IShieldModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.Deflector:
                    ((ModuleSlot<IDeflectorModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.Propulsion:
                    ((ModuleSlot<IPropulsionModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.Rotation:
                    ((ModuleSlot<IRotationModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.WarpDrive:
                    ((ModuleSlot<IWarpDriveModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.Hyperdrive:
                    ((ModuleSlot<IHyperdriveModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.Weapon:
                    ((ModuleSlot<IWeaponModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.Sensor:
                    ((ModuleSlot<ISensorModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.Transponder:
                    ((ModuleSlot<ITransponderModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.CargoBay:
                    ((ModuleSlot<ICargoBayModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.AICore:
                    ((ModuleSlot<IAICoreModule>)slot).Update(deltaTime);
                    break;
                case ModuleSlotType.LifeSupport:
                    ((ModuleSlot<ILifeSupportModule>)slot).Update(deltaTime);
                    break;
            }
        }
    }
}
