using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Defines the type of damage dealt by a weapon.
    /// </summary>
    public enum V2DamageType
    {
        Kinetic,
        Energy,
        Explosive,
        Thermal,
        EM
    }

    /// <summary>
    /// Configuration for weapon damage behavior including type and multipliers.
    /// </summary>
    [Serializable]
    public class V2WeaponDamageConfig
    {
        [Tooltip("The type of damage dealt.")]
        public V2DamageType damageType = V2DamageType.Energy;

        [Tooltip("Damage multiplier against shields. 1.0 = normal damage.")]
        [Range(0f, 3f)]
        public float shieldDamageMultiplier = 1f;

        [Tooltip("Damage multiplier against hull. 1.0 = normal damage.")]
        [Range(0f, 3f)]
        public float hullDamageMultiplier = 1f;

        [Tooltip("If true, damage completely ignores shields.")]
        public bool bypassesShield = false;

        [Tooltip("Percentage of damage that penetrates shields (0-1). Remaining damage hits shield.")]
        [Range(0f, 1f)]
        public float shieldPenetration = 0f;

        /// <summary>
        /// Default damage configuration with normal multipliers.
        /// </summary>
        public static V2WeaponDamageConfig Default => new V2WeaponDamageConfig
        {
            damageType = V2DamageType.Energy,
            shieldDamageMultiplier = 1f,
            hullDamageMultiplier = 1f,
            bypassesShield = false,
            shieldPenetration = 0f
        };

        /// <summary>
        /// Configuration optimized for shield damage.
        /// </summary>
        public static V2WeaponDamageConfig ShieldBuster => new V2WeaponDamageConfig
        {
            damageType = V2DamageType.EM,
            shieldDamageMultiplier = 2.5f,
            hullDamageMultiplier = 0.3f,
            bypassesShield = false,
            shieldPenetration = 0f
        };

        /// <summary>
        /// Configuration optimized for hull damage.
        /// </summary>
        public static V2WeaponDamageConfig ArmorPiercing => new V2WeaponDamageConfig
        {
            damageType = V2DamageType.Kinetic,
            shieldDamageMultiplier = 0.5f,
            hullDamageMultiplier = 1.5f,
            bypassesShield = false,
            shieldPenetration = 0f
        };

        /// <summary>
        /// Configuration with partial shield penetration.
        /// </summary>
        public static V2WeaponDamageConfig Penetrating => new V2WeaponDamageConfig
        {
            damageType = V2DamageType.Energy,
            shieldDamageMultiplier = 0.7f,
            hullDamageMultiplier = 1f,
            bypassesShield = false,
            shieldPenetration = 0.3f
        };
    }
}
