using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Static factory for spawning projectiles based on mode (Physics, RaycastBacked, Hitscan).
    /// </summary>
    public static class V2ProjectileSpawner
    {
        /// <summary>
        /// Spawns a projectile using the appropriate method based on projectile config mode.
        /// </summary>
        /// <param name="context">The spawn context containing all projectile data.</param>
        public static void Spawn(V2ProjectileSpawnContext context)
        {
            if (context.ProjectileConfig == null)
            {
                Debug.LogWarning("V2ProjectileSpawner.Spawn called with null ProjectileConfig");
                return;
            }

            switch (context.ProjectileConfig.mode)
            {
                case V2ProjectileMode.Physics:
                    SpawnPhysicsProjectile(context);
                    break;

                case V2ProjectileMode.RaycastBacked:
                    SpawnRaycastProjectile(context);
                    break;

                case V2ProjectileMode.Hitscan:
                    SpawnHitscanProjectile(context);
                    break;

                default:
                    Debug.LogWarning($"Unknown projectile mode: {context.ProjectileConfig.mode}");
                    break;
            }
        }

        private static void SpawnPhysicsProjectile(V2ProjectileSpawnContext context)
        {
            if (context.ProjectilePrefab == null)
            {
                Debug.LogWarning("[V2Spawner] Physics projectile mode requires a ProjectilePrefab");
                return;
            }

            var config = context.ProjectileConfig;

            Debug.Log($"[V2Spawner] Instantiating physics projectile at {context.SpawnPosition}, dir={context.Direction}, speed={config.speed}");

            // Instantiate prefab
            var projectileGO = Object.Instantiate(
                context.ProjectilePrefab,
                context.SpawnPosition,
                Quaternion.identity
            );

            Debug.Log($"[V2Spawner] Projectile instantiated: {projectileGO.name} at {projectileGO.transform.position}");

            // Scale
            projectileGO.transform.localScale = Vector3.one * config.scale;

            // Apply color to sprite if present
            var spriteRenderer = projectileGO.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = config.color;
            }

            // Attach trail if configured
            if (config.trailPrefab != null)
            {
                var trail = Object.Instantiate(config.trailPrefab, projectileGO.transform);
                trail.transform.localPosition = Vector3.zero;
            }

            // Get or add V2Projectile component
            var projectile = projectileGO.GetComponent<V2Projectile>();
            if (projectile == null)
            {
                Debug.Log("[V2Spawner] Adding V2Projectile component to prefab");
                projectile = projectileGO.AddComponent<V2Projectile>();
            }

            // Initialize projectile
            projectile.Initialize(
                owner: context.Owner,
                direction: context.Direction,
                speed: config.speed,
                damage: context.Damage,
                lifetime: config.lifetime,
                destroyOnHit: config.destroyOnHit,
                hitLayers: config.hitLayers,
                maxPenetrations: config.maxPenetrations,
                inheritedVelocity: config.inheritVelocity ? context.InheritedVelocity : Vector2.zero,
                damageConfig: context.DamageConfig,
                impactConfig: config.impactConfig
            );
        }

        private static void SpawnRaycastProjectile(V2ProjectileSpawnContext context)
        {
            var config = context.ProjectileConfig;

            // Perform raycast at fire time to determine hit point
            var hit = Physics2D.Raycast(
                context.SpawnPosition,
                context.Direction,
                config.speed * config.lifetime, // Max range based on speed and lifetime
                config.hitLayers
            );

            Vector2 targetPoint;
            Collider2D hitCollider = null;

            if (hit.collider != null)
            {
                // Check if we're not hitting owner
                if (context.Owner != null && hit.transform.IsChildOf(context.Owner.Transform))
                {
                    // Skip owner, extend to max range
                    targetPoint = context.SpawnPosition + context.Direction * (config.speed * config.lifetime);
                }
                // Check if we're hitting a projectile from the same owner
                else if (context.Owner != null)
                {
                    var hitProjectile = hit.collider.GetComponent<V2Projectile>();
                    if (hitProjectile != null && hitProjectile.Owner == context.Owner)
                    {
                        // Skip friendly projectile, extend to max range
                        targetPoint = context.SpawnPosition + context.Direction * (config.speed * config.lifetime);
                    }
                    else
                    {
                        targetPoint = hit.point;
                        hitCollider = hit.collider;
                    }
                }
                else
                {
                    targetPoint = hit.point;
                    hitCollider = hit.collider;
                }
            }
            else
            {
                targetPoint = context.SpawnPosition + context.Direction * (config.speed * config.lifetime);
            }

            // Create visual projectile that travels to predetermined point
            if (context.ProjectilePrefab != null)
            {
                SpawnRaycastVisualProjectile(context, targetPoint, hitCollider);
            }
        }

        private static void SpawnRaycastVisualProjectile(
            V2ProjectileSpawnContext context,
            Vector2 targetPoint,
            Collider2D hitCollider)
        {
            var config = context.ProjectileConfig;

            // Calculate travel time
            float distance = Vector2.Distance(context.SpawnPosition, targetPoint);
            float travelTime = distance / config.speed;

            // Spawn visual
            var projectileGO = Object.Instantiate(
                context.ProjectilePrefab,
                context.SpawnPosition,
                Quaternion.identity
            );

            projectileGO.transform.localScale = Vector3.one * config.scale;

            // Apply color
            var spriteRenderer = projectileGO.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = config.color;
            }

            // Rotate to face direction
            float angle = Mathf.Atan2(context.Direction.y, context.Direction.x) * Mathf.Rad2Deg - 90f;
            projectileGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Add raycast projectile behavior
            var behavior = projectileGO.AddComponent<V2RaycastProjectileBehavior>();
            behavior.Initialize(
                targetPoint,
                travelTime,
                context.Damage,
                context.DamageConfig,
                config.impactConfig,
                context.Owner,
                hitCollider
            );
        }

        private static void SpawnHitscanProjectile(V2ProjectileSpawnContext context)
        {
            var config = context.ProjectileConfig;

            float range = config.speed * config.lifetime;

            V2HitscanProjectile.Fire(
                context.SpawnPosition,
                context.Direction,
                range,
                context.Damage,
                config.hitLayers,
                context.Owner,
                context.DamageConfig,
                config
            );
        }
    }

    /// <summary>
    /// Behavior component for raycast-backed projectiles.
    /// Moves visual to predetermined hit point and applies damage on arrival.
    /// </summary>
    public class V2RaycastProjectileBehavior : MonoBehaviour
    {
        private Vector2 _startPosition;
        private Vector2 _targetPoint;
        private float _travelTime;
        private float _elapsed;
        private float _damage;
        private V2WeaponDamageConfig _damageConfig;
        private V2ImpactConfig _impactConfig;
        private IEntityController _owner;
        private Collider2D _hitCollider;
        private bool _damageApplied;

        public void Initialize(
            Vector2 targetPoint,
            float travelTime,
            float damage,
            V2WeaponDamageConfig damageConfig,
            V2ImpactConfig impactConfig,
            IEntityController owner,
            Collider2D hitCollider)
        {
            _startPosition = transform.position;
            _targetPoint = targetPoint;
            _travelTime = travelTime;
            _damage = damage;
            _damageConfig = damageConfig;
            _impactConfig = impactConfig;
            _owner = owner;
            _hitCollider = hitCollider;
            _elapsed = 0f;
            _damageApplied = false;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _travelTime);

            // Move towards target
            transform.position = Vector2.Lerp(_startPosition, _targetPoint, t);

            // Apply damage and effects when arrived
            if (t >= 1f && !_damageApplied)
            {
                _damageApplied = true;
                OnArrival();
                Destroy(gameObject);
            }
        }

        private void OnArrival()
        {
            if (_hitCollider != null)
            {
                // Apply damage
                var receiver = _hitCollider.GetComponentInParent<IV2DamageReceiver>();
                if (receiver != null)
                {
                    var damageInfo = new V2DamageInfo
                    {
                        BaseDamage = _damage,
                        DamageConfig = _damageConfig ?? V2WeaponDamageConfig.Default,
                        Source = _owner,
                        HitPoint = _targetPoint,
                        HitDirection = (_targetPoint - _startPosition).normalized
                    };
                    receiver.ReceiveDamage(damageInfo);
                }

                // Spawn impact effects
                SpawnImpactEffects();
            }
        }

        private void SpawnImpactEffects()
        {
            if (_impactConfig == null) return;

            Vector2 hitNormal = (_startPosition - _targetPoint).normalized;

            // Spawn particle effect
            if (_impactConfig.impactParticlePrefab != null)
            {
                var particles = Instantiate(
                    _impactConfig.impactParticlePrefab,
                    _targetPoint,
                    Quaternion.LookRotation(Vector3.forward, hitNormal)
                );
                particles.transform.localScale = Vector3.one * _impactConfig.particleScale;
                Destroy(particles, _impactConfig.effectDuration);
            }

            // Spawn impact light
            if (_impactConfig.spawnLight)
            {
                var lightGO = new GameObject("ImpactLight");
                lightGO.transform.position = (Vector3)_targetPoint;

                var light = lightGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
                light.color = _impactConfig.lightColor;
                light.intensity = _impactConfig.lightIntensity;
                light.pointLightOuterRadius = _impactConfig.lightRadius;

                Destroy(lightGO, _impactConfig.lightDuration);
            }

            // Play impact sound
            if (_impactConfig.impactSound != null)
            {
                AudioSource.PlayClipAtPoint(_impactConfig.impactSound, _targetPoint, _impactConfig.soundVolume);
            }
        }
    }
}
