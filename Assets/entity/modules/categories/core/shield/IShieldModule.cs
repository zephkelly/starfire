using System;
using Starfire.Entity.Modules.Damage;

namespace Starfire.Entity.Modules.Shield
{
    public enum ShieldState
    {
        Destroyed,      // Shield is broken, cannot regenerate until restored
        Inactive,       // Shield is offline (disabled)
        Charging,       // Initial charging after equip or repair
        RechargeDelay,  // Waiting for delay timer after taking damage
        Recharging,     // Actively regenerating shield points
        Active,         // Normal operational state
    }

    public interface IShieldModule : IEntityModule
    {
        int MaxShield { get; }
        int CurrentShield { get; set; }
        float RegenRate { get; }
        float RechargeDelay { get; }
        ShieldState State { get; }
        DamageResistances Resistances { get; }

        /// <summary>
        /// Process incoming damage to the shield.
        /// Returns the amount of damage that bleeds through to hull.
        /// </summary>
        float AbsorbDamage(DamageInfo damageInfo);

        /// <summary>
        /// Fired when shield is destroyed (reaches 0).
        /// </summary>
        event Action OnShieldDestroyed;

        /// <summary>
        /// Fired when shield finishes recharging to full.
        /// </summary>
        event Action OnShieldRestored;
    }
}
