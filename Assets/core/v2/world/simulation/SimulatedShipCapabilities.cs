using System;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Lightweight struct capturing module-derived capabilities for ships in background simulation.
    /// Extracted from ShipEntity modules at registration time to avoid carrying Unity references.
    /// </summary>
    [Serializable]
    public struct SimulatedShipCapabilities
    {
        // ── Defense (from IShipShieldModule) ─────────────────────────────────

        /// <summary>Maximum shield capacity from shield module.</summary>
        public float MaxShield;

        /// <summary>Shield regeneration rate (units per second).</summary>
        public float ShieldRegenRate;

        /// <summary>Delay before shield starts regenerating after damage (seconds).</summary>
        public float ShieldRechargeDelay;

        /// <summary>Current shield value (mutable during simulation).</summary>
        public float CurrentShield;

        // ── Hull (from IShipHullModule) ──────────────────────────────────────

        /// <summary>Maximum hull integrity from hull module.</summary>
        public float MaxIntegrity;

        /// <summary>Current hull integrity (mutable during simulation).</summary>
        public float CurrentIntegrity;

        // ── Combat (aggregated from IShipOffensiveModule[]) ──────────────────

        /// <summary>Total damage per second across all weapons (sum of Damage * FireRate).</summary>
        public float TotalDamagePerSecond;

        /// <summary>Maximum weapon range (longest range among all weapons).</summary>
        public float MaxWeaponRange;

        /// <summary>Number of offensive weapon modules.</summary>
        public int WeaponCount;

        // ── Flags ────────────────────────────────────────────────────────────

        /// <summary>Whether this ship has shields equipped.</summary>
        public bool HasShields => MaxShield > 0f;

        /// <summary>Whether this ship has weapons equipped.</summary>
        public bool HasWeapons => WeaponCount > 0;

        /// <summary>Whether shields are currently depleted.</summary>
        public bool ShieldsDepleted => HasShields && CurrentShield <= 0f;

        // ── Factory ──────────────────────────────────────────────────────────

        /// <summary>
        /// Default capabilities for ships without module data.
        /// Used as fallback when capabilities haven't been extracted.
        /// </summary>
        public static SimulatedShipCapabilities Default => new()
        {
            MaxShield = 0f,
            ShieldRegenRate = 0f,
            ShieldRechargeDelay = 3f,
            CurrentShield = 0f,
            MaxIntegrity = 100f,
            CurrentIntegrity = 100f,
            TotalDamagePerSecond = 0f,
            MaxWeaponRange = 0f,
            WeaponCount = 0
        };
    }
}
