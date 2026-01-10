using System;
using System.Collections.Generic;
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
    public class ShipSystems : EntitySystemsBase
    {
        private static readonly Dictionary<ModuleSlotType, Func<EntityControllerBase, IModuleSlot>> ShipSlotFactories = new()
        {
            { ModuleSlotType.Hull, c => new ModuleSlot<IHullModule>(c) },
            { ModuleSlotType.Shield, c => new ModuleSlot<IShieldModule>(c) },
            { ModuleSlotType.Deflector, c => new ModuleSlot<IDeflectorModule>(c) },
            { ModuleSlotType.Propulsion, c => new ModuleSlot<IPropulsionModule>(c) },
            { ModuleSlotType.Rotation, c => new ModuleSlot<IRotationModule>(c) },
            { ModuleSlotType.WarpDrive, c => new ModuleSlot<IWarpDriveModule>(c) },
            { ModuleSlotType.Hyperdrive, c => new ModuleSlot<IHyperdriveModule>(c) },
            { ModuleSlotType.Weapon, c => new ModuleSlot<IWeaponModule>(c) },
            { ModuleSlotType.Sensor, c => new ModuleSlot<ISensorModule>(c) },
            { ModuleSlotType.Transponder, c => new ModuleSlot<ITransponderModule>(c) },
            { ModuleSlotType.CargoBay, c => new ModuleSlot<ICargoBayModule>(c) },
            { ModuleSlotType.AICore, c => new ModuleSlot<IAICoreModule>(c) },
            { ModuleSlotType.LifeSupport, c => new ModuleSlot<ILifeSupportModule>(c) }
        };

        // Typed accessors for ship-specific modules
        public ModuleSlot<IHullModule> Hull => GetTypedSlot<IHullModule>(ModuleSlotType.Hull);
        public ModuleSlot<IShieldModule> Shield => GetTypedSlot<IShieldModule>(ModuleSlotType.Shield);
        public ModuleSlot<IDeflectorModule> Deflector => GetTypedSlot<IDeflectorModule>(ModuleSlotType.Deflector);
        public ModuleSlot<IPropulsionModule> Propulsion => GetTypedSlot<IPropulsionModule>(ModuleSlotType.Propulsion);
        public ModuleSlot<IRotationModule> Rotation => GetTypedSlot<IRotationModule>(ModuleSlotType.Rotation);
        public ModuleSlot<IWarpDriveModule> WarpDrive => GetTypedSlot<IWarpDriveModule>(ModuleSlotType.WarpDrive);
        public ModuleSlot<IHyperdriveModule> Hyperdrive => GetTypedSlot<IHyperdriveModule>(ModuleSlotType.Hyperdrive);
        public ModuleSlot<IWeaponModule> Weapon => GetTypedSlot<IWeaponModule>(ModuleSlotType.Weapon);
        public ModuleSlot<ISensorModule> Sensor => GetTypedSlot<ISensorModule>(ModuleSlotType.Sensor);
        public ModuleSlot<ITransponderModule> Transponder => GetTypedSlot<ITransponderModule>(ModuleSlotType.Transponder);
        public ModuleSlot<ICargoBayModule> CargoBay => GetTypedSlot<ICargoBayModule>(ModuleSlotType.CargoBay);
        public ModuleSlot<IAICoreModule> AICore => GetTypedSlot<IAICoreModule>(ModuleSlotType.AICore);
        public ModuleSlot<ILifeSupportModule> LifeSupport => GetTypedSlot<ILifeSupportModule>(ModuleSlotType.LifeSupport);

        public ShipSystems(EntityControllerBase controller, SlotConfiguration[] configurations)
            : base(controller, configurations)
        {
        }

        protected override Dictionary<ModuleSlotType, Func<EntityControllerBase, IModuleSlot>> GetSlotFactories()
        {
            return ShipSlotFactories;
        }
    }
}
