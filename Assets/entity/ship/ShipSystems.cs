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
        // Slot factories for the module type hierarchy
        private static readonly Dictionary<ModuleTypeId, Func<EntityControllerBase, IModuleSlot>> SlotFactories = new()
        {
            // Core > Structure
            { ModuleTypeId.Hull, c => new ModuleSlot<IHullModule>(c) },

            // Core > Defense
            { ModuleTypeId.Shield, c => new ModuleSlot<IShieldModule>(c) },
            { ModuleTypeId.Deflector, c => new ModuleSlot<IDeflectorModule>(c) },

            // Propulsion > Maneuvering
            { ModuleTypeId.ManeuveringThruster, c => new ModuleSlot<IPropulsionModule>(c) },
            { ModuleTypeId.RotationThruster, c => new ModuleSlot<IRotationModule>(c) },

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

        // === Typed accessors ===

        /// <summary>
        /// Gets all equipped weapon modules.
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

        // === Multi-slot primary accessors ===

        /// <summary>
        /// Gets all equipped rotation/maneuvering modules.
        /// </summary>
        public IEnumerable<IRotationModule> AllRotation => GetAllModulesOfType<IRotationModule>();

        /// <summary>
        /// Gets the primary (first) rotation module, or null if none equipped.
        /// </summary>
        public IRotationModule PrimaryRotation => AllRotation.FirstOrDefault();

        /// <summary>
        /// Gets all maneuvering thruster slots.
        /// </summary>
        public IReadOnlyList<IModuleSlot> ManeuveringThrusters =>
            GetSlotsBySubCategory(ModuleSubCategory.Maneuvering).ToList();

        /// <summary>
        /// Gets all rotation thruster slots specifically.
        /// </summary>
        public IReadOnlyList<IModuleSlot> RotationThrusters =>
            GetSlotsByType(ModuleTypeId.RotationThruster).ToList();

        /// <summary>
        /// Gets the primary hull module.
        /// </summary>
        public IHullModule PrimaryHull => GetAllModulesOfType<IHullModule>().FirstOrDefault();

        /// <summary>
        /// Gets the primary shield module.
        /// </summary>
        public IShieldModule PrimaryShield => GetAllModulesOfType<IShieldModule>().FirstOrDefault();

        /// <summary>
        /// Gets the primary impulse propulsion module.
        /// </summary>
        public IPropulsionModule PrimaryImpulse => GetAllModulesOfType<IPropulsionModule>().FirstOrDefault();

        /// <summary>
        /// Gets the primary AI core module.
        /// </summary>
        public IAICoreModule PrimaryAICore => GetAllModulesOfType<IAICoreModule>().FirstOrDefault();

        // === Constructor ===

        public ShipSystems(EntityControllerBase controller, MultiSlotConfiguration[] configurations)
            : base(controller, configurations)
        {
        }

        protected override Dictionary<ModuleTypeId, Func<EntityControllerBase, IModuleSlot>> GetSlotFactories()
        {
            return SlotFactories;
        }

        // === Utility methods ===

        /// <summary>
        /// Checks if the ship has any FTL capability (warp or hyperdrive).
        /// </summary>
        public bool HasFTLCapability =>
            HasModuleOfType(ModuleTypeId.WarpDrive) ||
            HasModuleOfType(ModuleTypeId.Hyperdrive);

        /// <summary>
        /// Gets the total weapon count (offensive + defensive).
        /// </summary>
        public int TotalWeaponCount => CountModulesInCategory(ModuleCategory.Weapons);
    }
}
