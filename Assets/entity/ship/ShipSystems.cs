using System;
using System.Collections.Generic;
using System.Linq;
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
        // Legacy single-slot factories (backwards compatible)
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

        // Multi-slot factories for the new hierarchy system
        private static readonly Dictionary<ModuleTypeId, Func<EntityControllerBase, IModuleSlot>> ShipMultiSlotFactories = new()
        {
            // Core > Structure
            { ModuleTypeId.Hull, c => new ModuleSlot<IHullModule>(c) },

            // Core > Defense
            { ModuleTypeId.Shield, c => new ModuleSlot<IShieldModule>(c) },
            { ModuleTypeId.Deflector, c => new ModuleSlot<IDeflectorModule>(c) },

            // Propulsion > Maneuvering
            { ModuleTypeId.ManeuveringThruster, c => new ModuleSlot<IRotationModule>(c) },

            // Propulsion > Impulse
            { ModuleTypeId.ImpulseEngine, c => new ModuleSlot<IPropulsionModule>(c) },

            // Propulsion > FTL
            { ModuleTypeId.WarpDrive, c => new ModuleSlot<IWarpDriveModule>(c) },
            { ModuleTypeId.Hyperdrive, c => new ModuleSlot<IHyperdriveModule>(c) },

            // Weapons > Offensive
            { ModuleTypeId.Laser, c => new ModuleSlot<IWeaponModule>(c) },
            { ModuleTypeId.PlasmaCannon, c => new ModuleSlot<IWeaponModule>(c) },
            { ModuleTypeId.MissileLauncher, c => new ModuleSlot<IWeaponModule>(c) },

            // Weapons > Defensive
            { ModuleTypeId.PointDefenseTurret, c => new ModuleSlot<IWeaponModule>(c) },

            // Systems > Sensors
            { ModuleTypeId.SensorArray, c => new ModuleSlot<ISensorModule>(c) },

            // Systems > Communications
            { ModuleTypeId.Transponder, c => new ModuleSlot<ITransponderModule>(c) },

            // Systems > Automation
            { ModuleTypeId.AICore, c => new ModuleSlot<IAICoreModule>(c) },

            // Utility > Storage
            { ModuleTypeId.CargoBay, c => new ModuleSlot<ICargoBayModule>(c) },

            // Utility > Support
            { ModuleTypeId.LifeSupport, c => new ModuleSlot<ILifeSupportModule>(c) }
        };

        // === Legacy typed accessors (backwards compatible) ===
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

        // === Multi-slot typed accessors ===

        /// <summary>
        /// Gets all equipped weapon modules (from both legacy and multi-slot systems).
        /// </summary>
        public IEnumerable<IWeaponModule> AllWeapons => GetAllModulesOfType<IWeaponModule>();

        /// <summary>
        /// Gets all equipped propulsion modules (impulse, warp, hyperdrive).
        /// </summary>
        public IEnumerable<IPropulsionModule> AllPropulsion => GetAllModulesOfType<IPropulsionModule>();

        /// <summary>
        /// Gets all slots containing offensive weapons.
        /// </summary>
        public IReadOnlyList<IModuleSlot> OffensiveWeapons =>
            GetSlotsBySubCategory(ModuleSubCategory.Offensive).ToList();

        /// <summary>
        /// Gets all slots containing defensive weapons (turrets, point defense).
        /// </summary>
        public IReadOnlyList<IModuleSlot> DefensiveWeapons =>
            GetSlotsBySubCategory(ModuleSubCategory.Defensive).ToList();

        /// <summary>
        /// Gets all FTL drive slots (warp, hyperdrive).
        /// </summary>
        public IReadOnlyList<IModuleSlot> FTLDrives =>
            GetSlotsBySubCategory(ModuleSubCategory.FTL).ToList();

        /// <summary>
        /// Gets all propulsion-related slots (maneuvering, impulse, FTL).
        /// </summary>
        public IReadOnlyList<IModuleSlot> PropulsionSlots =>
            GetSlotsByCategory(ModuleCategory.Propulsion).ToList();

        /// <summary>
        /// Gets all defensive system slots (shields, deflectors).
        /// </summary>
        public IReadOnlyList<IModuleSlot> DefensiveSystems =>
            GetSlotsBySubCategory(ModuleSubCategory.Defense).ToList();

        /// <summary>
        /// Gets the primary (first) weapon module, or null if none equipped.
        /// </summary>
        public IWeaponModule PrimaryWeapon => AllWeapons.FirstOrDefault();

        /// <summary>
        /// Gets the fastest propulsion module by max speed.
        /// </summary>
        public IPropulsionModule FastestPropulsion =>
            AllPropulsion.OrderByDescending(p => p.MaxSpeed).FirstOrDefault();

        // === Constructors ===

        /// <summary>
        /// Legacy constructor for backwards compatibility.
        /// </summary>
        public ShipSystems(EntityControllerBase controller, SlotConfiguration[] configurations)
            : base(controller, configurations)
        {
        }

        /// <summary>
        /// New constructor with multi-slot support.
        /// </summary>
        public ShipSystems(
            EntityControllerBase controller,
            SlotConfiguration[] legacyConfigurations,
            MultiSlotConfiguration[] multiConfigurations)
            : base(controller, legacyConfigurations, multiConfigurations)
        {
        }

        protected override Dictionary<ModuleSlotType, Func<EntityControllerBase, IModuleSlot>> GetSlotFactories()
        {
            return ShipSlotFactories;
        }

        protected override Dictionary<ModuleTypeId, Func<EntityControllerBase, IModuleSlot>> GetMultiSlotFactories()
        {
            return ShipMultiSlotFactories;
        }

        // === Utility methods ===

        /// <summary>
        /// Checks if the ship has any FTL capability (warp or hyperdrive).
        /// </summary>
        public bool HasFTLCapability =>
            HasModule(ModuleSlotType.WarpDrive) ||
            HasModule(ModuleSlotType.Hyperdrive) ||
            HasModuleOfType(ModuleTypeId.WarpDrive) ||
            HasModuleOfType(ModuleTypeId.Hyperdrive);

        /// <summary>
        /// Gets the total weapon count (offensive + defensive).
        /// </summary>
        public int TotalWeaponCount =>
            CountModulesInCategory(ModuleCategory.Weapons) +
            (HasModule(ModuleSlotType.Weapon) ? 1 : 0);
    }
}
