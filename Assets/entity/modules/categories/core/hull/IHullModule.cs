using System;
using Starfire.Entity.Modules.Damage;

namespace Starfire.Entity.Modules.Hull
{
    public interface IHullShipModule : IShipModule
    {
        int MaxHealth { get; }
        int CurrentHealth { get; set; }
        float DamageResistance { get; }
        DamageResistances TypeResistances { get; }
        bool IsDestroyed { get; }

        /// <summary>
        /// Process incoming damage to the hull.
        /// Returns actual damage dealt after resistances.
        /// </summary>
        float TakeDamage(DamageInfo damageInfo);

        /// <summary>
        /// Fired when hull is destroyed (reaches 0).
        /// </summary>
        event Action OnHullDestroyed;

        /// <summary>
        /// Fired when hull takes any damage.
        /// </summary>
        event Action<float> OnHullDamaged;
    }
}
