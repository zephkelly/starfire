using Starfire.Core.V3.Cam.Effects;
using StarfireV2.Pooling;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Physics-based projectile component. Attach to projectile prefabs.
    /// Uses Rigidbody2D for movement and trigger collisions for hit detection.
    /// Implements IPoolable for object pooling support.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class V2Projectile : MonoBehaviour, IPoolable
    {
        private IEntityController _owner;
        private float _damage;
        private V2WeaponDamageConfig _damageConfig;
        private V2ImpactConfig _impactConfig;
        private LayerMask _hitLayers;
        private bool _destroyOnHit;
        private int _remainingPenetrations;
        private int _initialPenetrations;
        private float _lifetime;
        private float _elapsedTime;

        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private TrailRenderer _trail;
        private SpriteRenderer _spriteRenderer;
        private bool _consumed;
        private bool _isActive;

        // Pool tracking
        private GameObject _sourcePrefab;
        private bool _usePooling = true;

        /// <summary>
        /// The entity controller that fired this projectile.
        /// </summary>
        public IEntityController Owner => _owner;

        /// <summary>
        /// Whether this projectile is currently active (not in pool).
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// The source prefab this projectile was instantiated from.
        /// Used for returning to the correct pool.
        /// </summary>
        public GameObject SourcePrefab => _sourcePrefab;

        #region IPoolable Implementation

        public bool OnPoolGet()
        {
            _isActive = true;
            _consumed = false;
            _elapsedTime = 0f;
            return true;
        }

        public void OnPoolReturn()
        {
            _isActive = false;
            ResetState();
        }

        private void ResetState()
        {
            _owner = null;
            _damage = 0f;
            _damageConfig = null;
            _impactConfig = null;
            _hitLayers = default;
            _destroyOnHit = true;
            _remainingPenetrations = 0;
            _lifetime = 0f;
            _elapsedTime = 0f;
            _consumed = false;

            // Reset physics
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }

            // Clear trail
            if (_trail == null)
            {
                _trail = GetComponent<TrailRenderer>();
            }

            if (_trail != null)
            {
                _trail.Clear();
            }

            // Reset transform
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        #endregion

        /// <summary>
        /// Sets the source prefab for pool return tracking.
        /// Call this immediately after getting from pool.
        /// </summary>
        public void SetSourcePrefab(GameObject prefab)
        {
            _sourcePrefab = prefab;
        }

        /// <summary>
        /// Disables pooling for this projectile instance.
        /// It will be destroyed instead of returned to pool.
        /// </summary>
        public void DisablePooling()
        {
            _usePooling = false;
        }

        /// <summary>
        /// Initializes the projectile with spawn context data.
        /// </summary>
        public void Initialize(
            IEntityController owner,
            Vector2 direction,
            float speed,
            float damage,
            float lifetime,
            bool destroyOnHit,
            LayerMask hitLayers,
            int maxPenetrations = 0,
            Vector2 inheritedVelocity = default,
            V2WeaponDamageConfig damageConfig = null,
            V2ImpactConfig impactConfig = null)
        {
            _owner = owner;
            _damage = damage;
            _damageConfig = damageConfig ?? V2WeaponDamageConfig.Default;
            _impactConfig = impactConfig;
            _hitLayers = hitLayers;
            _destroyOnHit = destroyOnHit;
            _remainingPenetrations = maxPenetrations;
            _initialPenetrations = maxPenetrations;
            _lifetime = lifetime;
            _elapsedTime = 0f;
            _consumed = false;
            _isActive = true;

            // Cache components
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }

            if (_trail == null)
            {
                _trail = GetComponent<TrailRenderer>();
            }

            if (_rigidbody == null)
            {
                Debug.LogError("[V2Projectile] No Rigidbody2D found on projectile!");
                return;
            }

            // Configure rigidbody
            _rigidbody.gravityScale = 0f;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Set velocity
            Vector2 velocity = direction.normalized * speed + inheritedVelocity;
            _rigidbody.linearVelocity = velocity;

            // Rotate to face direction of travel
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Ensure collider is trigger
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
            else
            {
                Debug.LogWarning("[V2Projectile] No Collider2D found on projectile!");
            }

            // Note: No longer using Destroy(gameObject, lifetime)
            // Lifetime is tracked in Update() for pooling support
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _lifetime)
            {
                ReturnToPool();
            }
        }

        /// <summary>
        /// Forces the projectile to be destroyed or returned to pool immediately.
        /// Called externally when the projectile should be removed (e.g., missile shot down by point defense).
        /// </summary>
        public void ForceDestroy()
        {
            ReturnToPool();
        }

        /// <summary>
        /// Returns this projectile to its pool, or destroys it if pooling is disabled.
        /// </summary>
        private void ReturnToPool()
        {
            if (_consumed) return;
            _consumed = true;

            if (_usePooling && ProjectilePoolManager.Instance != null && _sourcePrefab != null)
            {
                ProjectilePoolManager.Instance.ReturnPhysicsProjectile(gameObject, _sourcePrefab);
            }
            else
            {
                // Fallback to destroy if no pooling available
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_consumed && _destroyOnHit) return;

            // Check layer mask
            if ((_hitLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            // Prevent self-hitting owner
            if (_owner != null && other.transform.IsChildOf(_owner.Transform))
            {
                return;
            }

            // Prevent hitting projectiles from the same owner (friendly fire between weapons)
            var otherProjectile = other.GetComponent<V2Projectile>();
            if (otherProjectile != null && _owner != null && otherProjectile._owner == _owner)
            {
                return;
            }

            // Try to deal damage
            var damageReceiver = other.GetComponentInParent<IV2DamageReceiver>();
            if (damageReceiver != null)
            {
                ApplyDamage(damageReceiver, other);
            }

            // Spawn impact effects
            SpawnImpactEffects(other);

            // Handle penetration or destruction
            if (_remainingPenetrations > 0)
            {
                _remainingPenetrations--;
            }
            else if (_destroyOnHit)
            {
                ReturnToPool();
            }
        }

        private void ApplyDamage(IV2DamageReceiver receiver, Collider2D collider)
        {
            var damageInfo = new V2DamageInfo
            {
                BaseDamage = _damage,
                DamageConfig = _damageConfig,
                Source = _owner,
                HitPoint = collider.ClosestPoint(transform.position),
                HitDirection = _rigidbody.linearVelocity.normalized
            };

            receiver.ReceiveDamage(damageInfo);
        }

        private void SpawnImpactEffects(Collider2D collider)
        {
            if (_impactConfig == null) return;

            Vector2 hitPoint = collider.ClosestPoint(transform.position);
            Vector2 hitNormal = ((Vector2)transform.position - hitPoint).normalized;

            // Spawn particle effect
            if (_impactConfig.impactParticlePrefab != null)
            {
                var particles = Instantiate(
                    _impactConfig.impactParticlePrefab,
                    hitPoint,
                    Quaternion.LookRotation(Vector3.forward, hitNormal)
                );
                particles.transform.localScale = Vector3.one * _impactConfig.particleScale;
                Destroy(particles, _impactConfig.effectDuration);
            }

            // Spawn impact light
            if (_impactConfig.spawnLight)
            {
                SpawnImpactLight(hitPoint);
            }

            // Play impact sound
            if (_impactConfig.impactSound != null)
            {
                AudioSource.PlayClipAtPoint(_impactConfig.impactSound, hitPoint, _impactConfig.soundVolume);
            }

            // Trigger screen shake
            if (_impactConfig.screenShakeConfig != null)
            {
                V3CameraShakeService.Instance?.TriggerImpactShake(hitPoint, hitNormal, _impactConfig.screenShakeConfig);
            }
        }

        private void SpawnImpactLight(Vector2 position)
        {
            var lightGO = new GameObject("ImpactLight");
            lightGO.transform.position = position;

            var light = lightGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.color = _impactConfig.lightColor;
            light.intensity = _impactConfig.lightIntensity;
            light.pointLightOuterRadius = _impactConfig.lightRadius;

            Destroy(lightGO, _impactConfig.lightDuration);
        }
    }

    /// <summary>
    /// Interface for entities that can receive damage from V2 weapons.
    /// </summary>
    public interface IV2DamageReceiver
    {
        void ReceiveDamage(V2DamageInfo damageInfo);
    }

    /// <summary>
    /// Data container for damage information passed to damage receivers.
    /// </summary>
    public struct V2DamageInfo
    {
        public float BaseDamage;
        public V2WeaponDamageConfig DamageConfig;
        public IEntityController Source;
        public Vector2 HitPoint;
        public Vector2 HitDirection;

        /// <summary>
        /// Calculates final damage to shields using damage config multipliers.
        /// </summary>
        public float CalculateShieldDamage()
        {
            if (DamageConfig == null) return BaseDamage;
            if (DamageConfig.bypassesShield) return 0f;

            float shieldDamage = BaseDamage * DamageConfig.shieldDamageMultiplier;
            return shieldDamage * (1f - DamageConfig.shieldPenetration);
        }

        /// <summary>
        /// Calculates final damage to hull using damage config multipliers.
        /// </summary>
        public float CalculateHullDamage()
        {
            if (DamageConfig == null) return BaseDamage;
            return BaseDamage * DamageConfig.hullDamageMultiplier;
        }

        /// <summary>
        /// Calculates damage that penetrates shields and hits hull directly.
        /// </summary>
        public float CalculatePenetratingDamage()
        {
            if (DamageConfig == null) return 0f;
            if (DamageConfig.bypassesShield) return BaseDamage * DamageConfig.hullDamageMultiplier;

            float penetrating = BaseDamage * DamageConfig.shieldPenetration;
            return penetrating * DamageConfig.hullDamageMultiplier;
        }
    }
}
