using UnityEngine;

namespace Starfire.Entity.Modules.Damage
{
    /// <summary>
    /// Immutable data container for a single damage event.
    /// Contains all information needed to process damage.
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>Base damage amount before any modifiers.</summary>
        public readonly float BaseDamage;

        /// <summary>The type of damage for resistance calculations.</summary>
        public readonly DamageType Type;

        /// <summary>Multiplier applied to shield damage (1.0 = normal).</summary>
        public readonly float ShieldDamageMultiplier;

        /// <summary>Multiplier applied to hull damage (1.0 = normal).</summary>
        public readonly float HullDamageMultiplier;

        /// <summary>The entity that caused this damage (null for environmental).</summary>
        public readonly EntityControllerBase Source;

        /// <summary>World position where damage originated.</summary>
        public readonly Vector2 SourcePosition;

        /// <summary>Direction the damage came from (normalized).</summary>
        public readonly Vector2 Direction;

        /// <summary>If true, this damage bypasses shields entirely.</summary>
        public readonly bool BypassesShield;

        /// <summary>Percentage of damage that pierces through shields (0-1).</summary>
        public readonly float ShieldPenetration;

        public DamageInfo(
            float baseDamage,
            DamageType type,
            float shieldMultiplier = 1f,
            float hullMultiplier = 1f,
            EntityControllerBase source = null,
            Vector2 sourcePosition = default,
            Vector2 direction = default,
            bool bypassesShield = false,
            float shieldPenetration = 0f)
        {
            BaseDamage = baseDamage;
            Type = type;
            ShieldDamageMultiplier = shieldMultiplier;
            HullDamageMultiplier = hullMultiplier;
            Source = source;
            SourcePosition = sourcePosition;
            Direction = direction;
            BypassesShield = bypassesShield;
            ShieldPenetration = shieldPenetration;
        }

        /// <summary>
        /// Creates a simple damage info with just damage and type.
        /// </summary>
        public static DamageInfo Simple(float damage, DamageType type = DamageType.Kinetic)
        {
            return new DamageInfo(damage, type);
        }

        /// <summary>
        /// Creates a copy of this DamageInfo with modified base damage.
        /// Useful for applying resistance calculations.
        /// </summary>
        public DamageInfo WithBaseDamage(float newBaseDamage)
        {
            return new DamageInfo(
                newBaseDamage,
                Type,
                ShieldDamageMultiplier,
                HullDamageMultiplier,
                Source,
                SourcePosition,
                Direction,
                BypassesShield,
                ShieldPenetration
            );
        }
    }
}
