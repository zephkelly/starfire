using Starfire.Core.V3.Cam.Effects;
using StarfireV2.Pooling;
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

                case V2ProjectileMode.Missile:
                    SpawnMissileProjectile(context);
                    break;

                case V2ProjectileMode.ThrustMissile:
                    SpawnThrustMissileProjectile(context);
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

            GameObject projectileGO;
            bool fromPool = false;

            // Try to get from pool, fallback to instantiate
            if (ProjectilePoolManager.Instance != null)
            {
                projectileGO = ProjectilePoolManager.Instance.GetPhysicsProjectile(context.ProjectilePrefab);
                fromPool = true;
            }
            else
            {
                // Fallback: direct instantiation
                projectileGO = Object.Instantiate(
                    context.ProjectilePrefab,
                    context.SpawnPosition,
                    Quaternion.identity
                );
            }

            // Position (pool objects start at origin)
            projectileGO.transform.position = context.SpawnPosition;
            projectileGO.transform.rotation = Quaternion.identity;

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
                var existingTrail = projectileGO.GetComponent<TrailRenderer>();
                if (existingTrail == null)
                {
                    var trail = Object.Instantiate(config.trailPrefab, projectileGO.transform);
                    trail.transform.localPosition = Vector3.zero;
                }
                else
                {
                    // Reset existing trail for pooled projectiles
                    existingTrail.Clear();
                }
            }
            // Inline shader-based trail (when no prefab trail and config is enabled)
            else if (config.trailConfig != null && config.trailConfig.enabled)
            {
                var existingTrail = projectileGO.GetComponent<V2ProjectileTrail>();
                if (existingTrail == null)
                {
                    var trail = projectileGO.AddComponent<V2ProjectileTrail>();
                    trail.Initialize(config.trailConfig, config.color, config.scale, spriteRenderer);
                }
                else
                {
                    // Reinitialize existing trail for pooled projectiles
                    existingTrail.Reinitialize(config.trailConfig, config.color, config.scale, spriteRenderer);
                }
            }

            // Get or add V2Projectile component
            var projectile = projectileGO.GetComponent<V2Projectile>();
            if (projectile == null)
            {
                projectile = projectileGO.AddComponent<V2Projectile>();
                // Disable pooling for dynamically added components
                projectile.DisablePooling();
            }

            // Set source prefab for pool return tracking
            if (fromPool)
            {
                projectile.SetSourcePrefab(context.ProjectilePrefab);
            }
            else
            {
                // Disable pooling for non-pooled projectiles
                projectile.DisablePooling();
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

            GameObject projectileGO;
            bool fromPool = false;

            // Try to get from pool, fallback to instantiate
            if (ProjectilePoolManager.Instance != null)
            {
                projectileGO = ProjectilePoolManager.Instance.GetRaycastProjectile(context.ProjectilePrefab);
                fromPool = true;
            }
            else
            {
                projectileGO = Object.Instantiate(
                    context.ProjectilePrefab,
                    context.SpawnPosition,
                    Quaternion.identity
                );
            }

            // Position
            projectileGO.transform.position = context.SpawnPosition;
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

            // Inline shader-based trail for raycast projectiles
            if (config.trailConfig != null && config.trailConfig.enabled)
            {
                var existingTrail = projectileGO.GetComponent<V2ProjectileTrail>();
                if (existingTrail == null)
                {
                    var trail = projectileGO.AddComponent<V2ProjectileTrail>();
                    trail.Initialize(config.trailConfig, config.color, config.scale, spriteRenderer);
                }
                else
                {
                    // Reinitialize existing trail for pooled projectiles
                    existingTrail.Reinitialize(config.trailConfig, config.color, config.scale, spriteRenderer);
                }
            }

            // Get or add raycast projectile behavior
            var behavior = projectileGO.GetComponent<V2RaycastProjectileBehavior>();
            if (behavior == null)
            {
                behavior = projectileGO.AddComponent<V2RaycastProjectileBehavior>();
            }

            behavior.Initialize(
                targetPoint,
                travelTime,
                context.Damage,
                context.DamageConfig,
                config.impactConfig,
                context.Owner,
                hitCollider,
                fromPool ? context.ProjectilePrefab : null
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

        private static void SpawnMissileProjectile(V2ProjectileSpawnContext context)
        {
            if (context.ProjectilePrefab == null)
            {
                Debug.LogWarning("[V2Spawner] Missile mode requires a ProjectilePrefab");
                return;
            }

            var config = context.ProjectileConfig;
            var missileConfig = config.missileConfig;

            if (missileConfig == null)
            {
                Debug.LogWarning("[V2Spawner] Missile mode requires a MissileConfig. Falling back to Physics mode.");
                SpawnPhysicsProjectile(context);
                return;
            }

            // Calculate angled launch direction based on volley bloom
            float angleOffset = missileConfig.GetLaunchAngleOffset(context.MissileIndex, context.VolleyCount);
            Vector2 launchDirection = RotateVector(context.Direction, angleOffset);

            GameObject projectileGO;
            bool fromPool = false;

            // Try to get from pool, fallback to instantiate
            if (ProjectilePoolManager.Instance != null)
            {
                projectileGO = ProjectilePoolManager.Instance.GetPhysicsProjectile(context.ProjectilePrefab);
                fromPool = true;
            }
            else
            {
                projectileGO = Object.Instantiate(
                    context.ProjectilePrefab,
                    context.SpawnPosition,
                    Quaternion.identity
                );
            }

            // Position
            projectileGO.transform.position = context.SpawnPosition;

            // Initial rotation to face launch direction
            float angle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg - 90f;
            projectileGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Scale
            projectileGO.transform.localScale = Vector3.one * config.scale;

            // Apply color to sprite if present
            var spriteRenderer = projectileGO.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = config.color;
            }

            // Attach trail if configured (same as physics projectile)
            if (config.trailPrefab != null)
            {
                var existingTrail = projectileGO.GetComponent<TrailRenderer>();
                if (existingTrail == null)
                {
                    var trail = Object.Instantiate(config.trailPrefab, projectileGO.transform);
                    trail.transform.localPosition = Vector3.zero;
                }
                else
                {
                    existingTrail.Clear();
                }
            }
            else if (config.trailConfig != null && config.trailConfig.enabled)
            {
                var existingTrail = projectileGO.GetComponent<V2ProjectileTrail>();
                if (existingTrail == null)
                {
                    var trail = projectileGO.AddComponent<V2ProjectileTrail>();
                    trail.Initialize(config.trailConfig, config.color, config.scale, spriteRenderer);
                }
                else
                {
                    existingTrail.Reinitialize(config.trailConfig, config.color, config.scale, spriteRenderer);
                }
            }

            // Get or add V2Projectile component for collision/damage handling
            var projectile = projectileGO.GetComponent<V2Projectile>();
            if (projectile == null)
            {
                projectile = projectileGO.AddComponent<V2Projectile>();
                projectile.DisablePooling();
            }

            // Set source prefab for pool return tracking
            if (fromPool)
            {
                projectile.SetSourcePrefab(context.ProjectilePrefab);
            }
            else
            {
                projectile.DisablePooling();
            }

            // Calculate initial speed (reduced for bloom effect)
            float initialSpeed = config.speed * missileConfig.initialSpeedMultiplier;

            // Initialize base projectile for collision handling
            projectile.Initialize(
                owner: context.Owner,
                direction: launchDirection,
                speed: initialSpeed,
                damage: context.Damage,
                lifetime: config.lifetime,
                destroyOnHit: config.destroyOnHit,
                hitLayers: config.hitLayers,
                maxPenetrations: config.maxPenetrations,
                inheritedVelocity: config.inheritVelocity ? context.InheritedVelocity : Vector2.zero,
                damageConfig: context.DamageConfig,
                impactConfig: config.impactConfig
            );

            // Add or get missile behavior component for homing/weaving
            var missileBehavior = projectileGO.GetComponent<V2MissileBehavior>();
            if (missileBehavior == null)
            {
                missileBehavior = projectileGO.AddComponent<V2MissileBehavior>();
            }

            missileBehavior.Initialize(
                context.Owner,
                missileConfig,
                config.speed, // Full base speed for acceleration curve
                config.lifetime,
                context.PreLockedTarget
            );
        }

        private static void SpawnThrustMissileProjectile(V2ProjectileSpawnContext context)
        {
            if (context.ProjectilePrefab == null)
            {
                Debug.LogWarning("[V2Spawner] ThrustMissile mode requires a ProjectilePrefab");
                return;
            }

            var config = context.ProjectileConfig;
            var thrustConfig = config.thrustMissileConfig;

            if (thrustConfig == null)
            {
                Debug.LogWarning("[V2Spawner] ThrustMissile mode requires a ThrustMissileConfig. Falling back to Physics mode.");
                SpawnPhysicsProjectile(context);
                return;
            }

            // Calculate angled launch direction based on volley bloom
            float angleOffset = thrustConfig.GetLaunchAngleOffset(context.MissileIndex, context.VolleyCount);
            Vector2 launchDirection = RotateVector(context.Direction, angleOffset);

            GameObject projectileGO;
            bool fromPool = false;

            if (ProjectilePoolManager.Instance != null)
            {
                projectileGO = ProjectilePoolManager.Instance.GetPhysicsProjectile(context.ProjectilePrefab);
                fromPool = true;
            }
            else
            {
                projectileGO = Object.Instantiate(
                    context.ProjectilePrefab,
                    context.SpawnPosition,
                    Quaternion.identity
                );
            }

            // Position and rotation
            projectileGO.transform.position = context.SpawnPosition;
            float angle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg - 90f;
            projectileGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            projectileGO.transform.localScale = Vector3.one * config.scale;

            // Apply color
            var spriteRenderer = projectileGO.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = config.color;
            }

            // Trail setup
            if (config.trailPrefab != null)
            {
                var existingTrail = projectileGO.GetComponent<TrailRenderer>();
                if (existingTrail == null)
                {
                    var trail = Object.Instantiate(config.trailPrefab, projectileGO.transform);
                    trail.transform.localPosition = Vector3.zero;
                }
                else
                {
                    existingTrail.Clear();
                }
            }
            else if (config.trailConfig != null && config.trailConfig.enabled)
            {
                var existingTrail = projectileGO.GetComponent<V2ProjectileTrail>();
                if (existingTrail == null)
                {
                    var trail = projectileGO.AddComponent<V2ProjectileTrail>();
                    trail.Initialize(config.trailConfig, config.color, config.scale, spriteRenderer);
                }
                else
                {
                    existingTrail.Reinitialize(config.trailConfig, config.color, config.scale, spriteRenderer);
                }
            }

            // Configure rigidbody physics
            var rigidbody = projectileGO.GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.mass = thrustConfig.missileMass;
                rigidbody.linearDamping = thrustConfig.missileLinearDrag;
                rigidbody.angularDamping = thrustConfig.missileAngularDrag;
            }

            // Get or add V2Projectile component
            var projectile = projectileGO.GetComponent<V2Projectile>();
            if (projectile == null)
            {
                projectile = projectileGO.AddComponent<V2Projectile>();
                projectile.DisablePooling();
            }

            if (fromPool)
            {
                projectile.SetSourcePrefab(context.ProjectilePrefab);
            }
            else
            {
                projectile.DisablePooling();
            }

            // Initialize base projectile with reduced initial speed
            float initialSpeed = config.speed * thrustConfig.initialVelocityMultiplier;

            projectile.Initialize(
                owner: context.Owner,
                direction: launchDirection,
                speed: initialSpeed,
                damage: context.Damage,
                lifetime: config.lifetime,
                destroyOnHit: config.destroyOnHit,
                hitLayers: config.hitLayers,
                maxPenetrations: config.maxPenetrations,
                inheritedVelocity: config.inheritVelocity ? context.InheritedVelocity : Vector2.zero,
                damageConfig: context.DamageConfig,
                impactConfig: config.impactConfig
            );

            // Add or get thrust missile behavior
            var thrustBehavior = projectileGO.GetComponent<V2ThrustMissileBehavior>();
            if (thrustBehavior == null)
            {
                thrustBehavior = projectileGO.AddComponent<V2ThrustMissileBehavior>();
            }

            thrustBehavior.Initialize(
                context.Owner,
                thrustConfig,
                config.speed,
                config.lifetime,
                context.PreLockedTarget
            );
        }

        /// <summary>
        /// Rotates a 2D vector by the specified angle in degrees.
        /// </summary>
        private static Vector2 RotateVector(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }
    }

    /// <summary>
    /// Behavior component for raycast-backed projectiles.
    /// Moves visual to predetermined hit point and applies damage on arrival.
    /// Supports object pooling.
    /// </summary>
    public class V2RaycastProjectileBehavior : MonoBehaviour, IPoolable
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
        private bool _isActive;
        private GameObject _sourcePrefab;

        public bool IsActive => _isActive;

        public bool OnPoolGet()
        {
            _isActive = true;
            _damageApplied = false;
            _elapsed = 0f;
            return true;
        }

        public void OnPoolReturn()
        {
            _isActive = false;
            _owner = null;
            _hitCollider = null;
            _damageConfig = null;
            _impactConfig = null;
            _sourcePrefab = null;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        public void Initialize(
            Vector2 targetPoint,
            float travelTime,
            float damage,
            V2WeaponDamageConfig damageConfig,
            V2ImpactConfig impactConfig,
            IEntityController owner,
            Collider2D hitCollider,
            GameObject sourcePrefab = null)
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
            _isActive = true;
            _sourcePrefab = sourcePrefab;
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _travelTime);

            // Move towards target
            transform.position = Vector2.Lerp(_startPosition, _targetPoint, t);

            // Apply damage and effects when arrived
            if (t >= 1f && !_damageApplied)
            {
                _damageApplied = true;
                OnArrival();
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (_sourcePrefab != null && ProjectilePoolManager.Instance != null)
            {
                ProjectilePoolManager.Instance.ReturnRaycastProjectile(gameObject, _sourcePrefab);
            }
            else
            {
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

            // Trigger screen shake
            if (_impactConfig.screenShakeConfig != null)
            {
                V3CameraShakeService.Instance?.TriggerImpactShake(_targetPoint, hitNormal, _impactConfig.screenShakeConfig);
            }
        }
    }
}
