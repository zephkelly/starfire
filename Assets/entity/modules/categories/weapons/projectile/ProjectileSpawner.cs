using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Factory class that spawns the appropriate projectile type based on configuration.
    /// </summary>
    public static class ProjectileSpawner
    {
        /// <summary>
        /// Spawns a projectile using the mode specified in the ProjectileConfig.
        /// </summary>
        public static void Spawn(ProjectileSpawnContext context)
        {
            var mode = context.ProjectileConfig?.projectileMode ?? ProjectileMode.Physics;

            switch (mode)
            {
                case ProjectileMode.Physics:
                    SpawnPhysicsProjectile(context);
                    break;

                case ProjectileMode.RaycastBacked:
                    SpawnRaycastProjectile(context);
                    break;

                case ProjectileMode.Hitscan:
                    SpawnHitscanProjectile(context);
                    break;

                default:
                    Debug.LogWarning($"Unknown ProjectileMode: {mode}. Falling back to Physics.");
                    SpawnPhysicsProjectile(context);
                    break;
            }
        }

        private static void SpawnPhysicsProjectile(ProjectileSpawnContext context)
        {
            if (context.ProjectilePrefab == null)
            {
                Debug.LogWarning("Cannot spawn Physics projectile: no prefab assigned");
                return;
            }

            // Instantiate the prefab
            var projectileGO = Object.Instantiate(
                context.ProjectilePrefab,
                context.SpawnPosition,
                Quaternion.identity
            );

            var projectile = projectileGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                var config = context.ProjectileConfig;

                projectile.Initialize(
                    owner: context.Owner,
                    direction: context.Direction,
                    speed: config.speed,
                    damage: context.Damage,
                    lifetime: config.lifetime,
                    destroyOnHit: config.destroyOnHit,
                    hitLayers: config.hitLayers,
                    inheritedVelocity: context.InheritedVelocity,
                    damageConfig: context.DamageConfig,
                    impactConfig: config.impactConfig
                );

                projectile.ApplyVisualConfig(config.scale, config.color);
            }
            else
            {
                Debug.LogWarning($"Projectile prefab is missing Projectile component");
                Object.Destroy(projectileGO);
            }
        }

        private static void SpawnRaycastProjectile(ProjectileSpawnContext context)
        {
            var go = new GameObject("RaycastProjectile");
            go.transform.position = context.SpawnPosition;

            var behavior = go.AddComponent<RaycastProjectile>();
            behavior.Initialize(context);
        }

        private static void SpawnHitscanProjectile(ProjectileSpawnContext context)
        {
            var go = new GameObject("HitscanProjectile");
            go.transform.position = context.SpawnPosition;

            var behavior = go.AddComponent<HitscanProjectile>();
            behavior.Initialize(context);
        }
    }
}
