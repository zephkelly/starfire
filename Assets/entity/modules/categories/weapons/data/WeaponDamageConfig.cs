using System;
using Starfire.Entity.Modules.Damage;
using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Configuration for weapon damage characteristics.
    /// Determines how damage is distributed between shields and hull.
    /// </summary>
    [Serializable]
    public class WeaponDamageConfig
    {
        [Header("Damage Type")]
        [Tooltip("Primary damage type of this weapon. Affects resistance calculations.")]
        public DamageType damageType = DamageType.Energy;

        [Header("Target Multipliers")]
        [Tooltip("Damage multiplier when hitting shields (1.0 = normal, 2.0 = double damage to shields)")]
        [Range(0f, 3f)]
        public float shieldDamageMultiplier = 1f;

        [Tooltip("Damage multiplier when hitting hull (1.0 = normal, 0.5 = half damage to hull)")]
        [Range(0f, 3f)]
        public float hullDamageMultiplier = 1f;

        [Header("Special Effects")]
        [Tooltip("If true, damage bypasses shields entirely and hits hull directly")]
        public bool bypassesShield = false;

        /// <summary>
        /// Creates a default damage config with Energy type and normal multipliers.
        /// </summary>
        public static WeaponDamageConfig Default => new WeaponDamageConfig();

        /// <summary>
        /// Creates a shield buster config (high shield damage, low hull damage).
        /// </summary>
        public static WeaponDamageConfig ShieldBuster => new WeaponDamageConfig
        {
            damageType = DamageType.Electromagnetic,
            shieldDamageMultiplier = 2.5f,
            hullDamageMultiplier = 0.3f,
            bypassesShield = false
        };

        /// <summary>
        /// Creates an armor piercing config (low shield damage, high hull damage).
        /// </summary>
        public static WeaponDamageConfig ArmorPiercing => new WeaponDamageConfig
        {
            damageType = DamageType.Kinetic,
            shieldDamageMultiplier = 0.5f,
            hullDamageMultiplier = 1.5f,
            bypassesShield = false
        };
    }
}
