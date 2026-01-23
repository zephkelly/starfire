using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Data container passed to V2ProjectileSpawner containing all information needed to spawn a projectile.
    /// </summary>
    public struct V2ProjectileSpawnContext
    {
        /// <summary>
        /// The entity controller that fired this projectile. Used for damage attribution and collision filtering.
        /// </summary>
        public IEntityController Owner;

        /// <summary>
        /// World-space position where the projectile spawns.
        /// </summary>
        public Vector2 SpawnPosition;

        /// <summary>
        /// Normalized direction the projectile travels.
        /// </summary>
        public Vector2 Direction;

        /// <summary>
        /// Velocity inherited from the firing ship (if inheritVelocity is enabled).
        /// </summary>
        public Vector2 InheritedVelocity;

        /// <summary>
        /// Base damage value for this projectile.
        /// </summary>
        public float Damage;

        /// <summary>
        /// Configuration for damage types and multipliers.
        /// </summary>
        public V2WeaponDamageConfig DamageConfig;

        /// <summary>
        /// Configuration for projectile behavior (speed, lifetime, mode, etc.).
        /// </summary>
        public V2ProjectileConfig ProjectileConfig;

        /// <summary>
        /// Prefab to instantiate for Physics mode projectiles.
        /// </summary>
        public GameObject ProjectilePrefab;
    }
}
