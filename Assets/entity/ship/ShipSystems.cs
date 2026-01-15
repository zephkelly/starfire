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
            { ModuleTypeId.Hull, c => new ModuleSlot<IHullShipModule>(c) },

            // Core > Defense
            { ModuleTypeId.Shield, c => new ModuleSlot<IShieldShipModule>(c) },
            { ModuleTypeId.Deflector, c => new ModuleSlot<IDeflectorShipModule>(c) },

            // Propulsion > Maneuvering
            { ModuleTypeId.ManeuveringThruster, c => new ModuleSlot<IPropulsionShipModule>(c) },
            { ModuleTypeId.RotationThruster, c => new ModuleSlot<IRotationShipModule>(c) },

            // Propulsion > Impulse
            { ModuleTypeId.ImpulseEngine, c => new ModuleSlot<IPropulsionShipModule>(c) },

            // Propulsion > FTL
            { ModuleTypeId.WarpDrive, c => new ModuleSlot<IWarpDriveShipModule>(c) },
            { ModuleTypeId.Hyperdrive, c => new ModuleSlot<IHyperdriveShipModule>(c) },

            // Weapons > Offensive
            { ModuleTypeId.Laser, c => new ModuleSlot<IWeaponShipModule>(c) },
            { ModuleTypeId.PlasmaCannon, c => new ModuleSlot<IWeaponShipModule>(c) },
            { ModuleTypeId.MissileLauncher, c => new ModuleSlot<IWeaponShipModule>(c) },

            // Weapons > Defensive
            { ModuleTypeId.PointDefenseTurret, c => new ModuleSlot<IWeaponShipModule>(c) },

            // Systems > Sensors
            { ModuleTypeId.SensorArray, c => new ModuleSlot<ISensorShipModule>(c) },

            // Systems > Communications
            { ModuleTypeId.Transponder, c => new ModuleSlot<ITransponderShipModule>(c) },

            // Systems > Automation
            { ModuleTypeId.AICore, c => new ModuleSlot<IAICoreShipModule>(c) },

            // Utility > Storage
            { ModuleTypeId.CargoBay, c => new ModuleSlot<ICargoBayShipModule>(c) },

            // Utility > Support
            { ModuleTypeId.LifeSupport, c => new ModuleSlot<ILifeSupportShipModule>(c) }
        };

        // === Typed accessors ===

        /// <summary>
        /// Gets all equipped weapon modules.
        /// </summary>
        public IEnumerable<IWeaponShipModule> AllWeapons => GetAllModulesOfType<IWeaponShipModule>();

        /// <summary>
        /// Gets all equipped propulsion modules (impulse, warp, hyperdrive).
        /// </summary>
        public IEnumerable<IPropulsionShipModule> AllPropulsion => GetAllModulesOfType<IPropulsionShipModule>();

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
        public IWeaponShipModule PrimaryWeapon => AllWeapons.FirstOrDefault();

        /// <summary>
        /// Gets the fastest propulsion module by max speed.
        /// </summary>
        public IPropulsionShipModule FastestPropulsion =>
            AllPropulsion.OrderByDescending(p => p.MaxSpeed).FirstOrDefault();

        // === Multi-slot primary accessors ===

        /// <summary>
        /// Gets all equipped rotation/maneuvering modules.
        /// </summary>
        public IEnumerable<IRotationShipModule> AllRotation => GetAllModulesOfType<IRotationShipModule>();

        /// <summary>
        /// Gets the primary (first) rotation module, or null if none equipped.
        /// </summary>
        public IRotationShipModule PrimaryRotation => AllRotation.FirstOrDefault();

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
        public IHullShipModule PrimaryHull => GetAllModulesOfType<IHullShipModule>().FirstOrDefault();

        /// <summary>
        /// Gets the primary shield module.
        /// </summary>
        public IShieldShipModule PrimaryShield => GetAllModulesOfType<IShieldShipModule>().FirstOrDefault();

        /// <summary>
        /// Gets the primary impulse propulsion module.
        /// </summary>
        public IPropulsionShipModule PrimaryImpulse => GetAllModulesOfType<IPropulsionShipModule>().FirstOrDefault();

        /// <summary>
        /// Gets the primary AI core module.
        /// </summary>
        public IAICoreShipModule PrimaryAICore => GetAllModulesOfType<IAICoreShipModule>().FirstOrDefault();

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
