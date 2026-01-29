using UnityEngine;

namespace StarfireV2.Pooling
{
    /// <summary>
    /// Configuration for the projectile pooling system.
    /// Controls pool sizing, expansion, and cleanup behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectilePoolConfig", menuName = "StarfireV2/Pooling/ProjectilePoolConfig")]
    public class ProjectilePoolConfig : ScriptableObject
    {
        [Header("Default Pool Sizes")]
        [Tooltip("Default initial pool size per prefab type when no weapon data is available.")]
        public int defaultInitialSize = 20;

        [Tooltip("Number of instances to add when pool is empty.")]
        public int expansionStep = 10;

        [Tooltip("Maximum pool size per prefab type. 0 = unlimited.")]
        public int maxPoolSize = 500;

        [Header("Entity-Based Sizing")]
        [Tooltip("Base projectiles to pool per weapon instance.")]
        public int projectilesPerWeapon = 10;

        [Tooltip("Multiplier for pool size calculation: poolSize = fireRate * lifetime * this value.")]
        public float fireRateMultiplier = 2f;

        [Tooltip("Minimum pool size per weapon type regardless of fire rate.")]
        public int minPoolPerWeapon = 5;

        [Header("Hitscan Beams")]
        [Tooltip("Initial pool size for hitscan beam visuals.")]
        public int hitscanBeamPoolSize = 30;

        [Header("Pool Maintenance")]
        [Tooltip("Seconds of inactivity before considering pool shrinking.")]
        public float shrinkDelay = 30f;

        [Tooltip("Pool utilization threshold below which shrinking occurs. 0.3 = shrink if less than 30% of pool is used.")]
        [Range(0.1f, 0.9f)]
        public float shrinkThreshold = 0.3f;

        [Tooltip("Whether to shrink pools when scenes are unloaded.")]
        public bool shrinkOnSceneUnload = true;

        [Header("Debug")]
        [Tooltip("Log pool events (get, return, expand, shrink).")]
        public bool logPoolEvents = false;

        /// <summary>
        /// Calculates the optimal pool size for a weapon configuration.
        /// </summary>
        /// <param name="fireRate">Shots per second.</param>
        /// <param name="lifetime">Projectile lifetime in seconds.</param>
        /// <param name="entityCount">Number of entities with this weapon.</param>
        /// <returns>Recommended pool size.</returns>
        public int CalculatePoolSize(float fireRate, float lifetime, int entityCount)
        {
            // Calculate max projectiles in flight per entity
            // fireRate * lifetime gives theoretical max, multiplier adds buffer
            int projectilesPerEntity = Mathf.CeilToInt(fireRate * lifetime * fireRateMultiplier);

            // Ensure minimum
            projectilesPerEntity = Mathf.Max(projectilesPerEntity, minPoolPerWeapon);

            // Scale by entity count
            int totalNeeded = projectilesPerEntity * entityCount;

            // Apply maximum cap if configured
            if (maxPoolSize > 0)
            {
                totalNeeded = Mathf.Min(totalNeeded, maxPoolSize);
            }

            return totalNeeded;
        }
    }
}
