namespace Starfire.Entity.Modules.Damage
{
    /// <summary>
    /// Result of processing a damage event, useful for UI feedback and effects.
    /// </summary>
    public readonly struct DamageResult
    {
        /// <summary>The original damage info that was processed.</summary>
        public readonly DamageInfo OriginalDamage;

        /// <summary>Actual damage absorbed by shields.</summary>
        public readonly float ShieldDamageDealt;

        /// <summary>Actual damage dealt to hull.</summary>
        public readonly float HullDamageDealt;

        /// <summary>Damage that bled through shields to hull.</summary>
        public readonly float BleedthroughDamage;

        /// <summary>Whether shields were broken by this damage.</summary>
        public readonly bool ShieldsDestroyed;

        /// <summary>Whether hull was destroyed by this damage.</summary>
        public readonly bool EntityDestroyed;

        public DamageResult(
            DamageInfo originalDamage,
            float shieldDamage,
            float hullDamage,
            float bleedthrough,
            bool shieldsDestroyed,
            bool entityDestroyed)
        {
            OriginalDamage = originalDamage;
            ShieldDamageDealt = shieldDamage;
            HullDamageDealt = hullDamage;
            BleedthroughDamage = bleedthrough;
            ShieldsDestroyed = shieldsDestroyed;
            EntityDestroyed = entityDestroyed;
        }

        /// <summary>Total damage dealt (shield + hull).</summary>
        public float TotalDamageDealt => ShieldDamageDealt + HullDamageDealt;

        /// <summary>Whether any damage was dealt.</summary>
        public bool DamageWasDealt => TotalDamageDealt > 0f;

        /// <summary>Creates an empty result (no damage dealt).</summary>
        public static DamageResult None(DamageInfo originalDamage)
        {
            return new DamageResult(originalDamage, 0f, 0f, 0f, false, false);
        }
    }
}
