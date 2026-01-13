using System;

namespace Starfire.Entity.Modules.Damage
{
    /// <summary>
    /// Contract for any entity that can receive damage.
    /// Implemented by DamageProcessor or similar damage handling components.
    /// </summary>
    public interface IDamageReceiver
    {
        /// <summary>
        /// The entity controller that owns this damage receiver.
        /// </summary>
        EntityControllerBase Controller { get; }

        /// <summary>
        /// Whether this entity can currently receive damage.
        /// Returns false if invulnerable, dead, or otherwise immune.
        /// </summary>
        bool CanReceiveDamage { get; }

        /// <summary>
        /// Process incoming damage and return the result.
        /// Handles shield absorption, bleedthrough, and hull damage.
        /// </summary>
        DamageResult ReceiveDamage(DamageInfo damageInfo);

        /// <summary>
        /// Fired when damage is received (after processing).
        /// Use for damage numbers, hit effects, sound, etc.
        /// </summary>
        event Action<DamageResult> OnDamageReceived;

        /// <summary>
        /// Fired when the entity is destroyed (hull reaches 0).
        /// Use for death handling, explosions, drops, etc.
        /// </summary>
        event Action<DamageResult> OnDestroyed;
    }
}
