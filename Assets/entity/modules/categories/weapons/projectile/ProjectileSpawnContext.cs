using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Contains all parameters needed to spawn a projectile.
    /// Passed to ProjectileSpawner to create the appropriate projectile type.
    /// </summary>
    public struct ProjectileSpawnContext
    {
        /// <summary>
        /// The entity that fired this projectile (used for damage attribution and self-hit prevention).
        /// </summary>
        public EntityControllerBase Owner;

        /// <summary>
        /// World position where the projectile spawns.
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
        /// Damage type configuration (multipliers, bypass settings).
        /// </summary>
        public WeaponDamageConfig DamageConfig;

        /// <summary>
        /// Projectile configuration (speed, lifetime, collision mode, visual settings).
        /// </summary>
        public ProjectileConfig ProjectileConfig;

        /// <summary>
        /// Prefab to instantiate for Physics mode. May be null for non-Physics modes.
        /// </summary>
        public GameObject ProjectilePrefab;
    }
}
