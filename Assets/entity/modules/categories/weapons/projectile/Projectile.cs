using Starfire.Entity.Modules.Damage;
using Starfire.Entity.Modules.Shield;
using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Basic projectile behavior. Moves in a direction and detects collisions.
    /// Attach to a prefab with Rigidbody2D and Collider2D (set as trigger).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        private Rigidbody2D _rigidbody;
        private float _damage;
        private float _lifetime;
        private float _spawnTime;
        private EntityControllerBase _owner;
        private bool _destroyOnHit;
        private LayerMask _hitLayers;
        private WeaponDamageConfig _damageConfig;
        private ImpactConfig _impactConfig;
        private bool _isConsumed;

        public EntityControllerBase Owner => _owner;
        public float Damage => _damage;
        public ImpactConfig ImpactConfig => _impactConfig;
        public WeaponDamageConfig DamageConfig => _damageConfig;

        /// <summary>
        /// Marks this projectile as consumed (already processed by shield).
        /// Prevents double-damage from queued collision events.
        /// </summary>
        public bool IsConsumed
        {
            get => _isConsumed;
            set => _isConsumed = value;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = GetComponent<Collider2D>();
            collider.isTrigger = true;
        }

        /// <summary>
        /// Initialize the projectile with movement and damage parameters.
        /// </summary>
        public void Initialize(
            EntityControllerBase owner,
            Vector2 direction,
            float speed,
            float damage,
            float lifetime,
            bool destroyOnHit,
            LayerMask hitLayers,
            Vector2 inheritedVelocity = default,
            WeaponDamageConfig damageConfig = null,
            ImpactConfig impactConfig = null)
        {
            _owner = owner;
            _damage = damage;
            _lifetime = lifetime;
            _destroyOnHit = destroyOnHit;
            _hitLayers = hitLayers;
            _spawnTime = Time.time;
            _damageConfig = damageConfig ?? new WeaponDamageConfig();
            _impactConfig = impactConfig;

            // Set velocity
            Vector2 velocity = direction.normalized * speed + inheritedVelocity;
            _rigidbody.linearVelocity = velocity;

            // Rotate to face direction of travel
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// Apply visual configuration to the projectile.
        /// </summary>
        public void ApplyVisualConfig(float scale, Color color)
        {
            transform.localScale = Vector3.one * scale;

            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        private void Update()
        {
            // Lifetime check
            if (Time.time - _spawnTime >= _lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Already consumed by shield - don't process further
            if (_isConsumed) return;

            Debug.Log("Projectile OnTriggerEnter2D with " + other.gameObject.name);
            // Check if we should interact with this layer
            if (_hitLayers != 0 && (_hitLayers & (1 << other.gameObject.layer)) == 0)
            {
                Debug.Log($"[Projectile] Layer check failed - ignoring collision");
                return;
            }

            // Don't hit owner
            var otherController = other.GetComponentInParent<EntityControllerBase>();
            if (otherController != null && otherController == _owner)
            {
                return;
            }

            // Let ShieldBoundary handle shield collisions
            var shieldBoundary = other.GetComponent<ShieldBoundary>();
            if (shieldBoundary != null)
            {
                Debug.Log($"[Projectile] Collided with ShieldBoundary of {otherController?.name}");
                return;
            }

            // Calculate actual surface impact point
            Vector2 hitPoint = other.ClosestPoint(transform.position);

            // Apply damage if target has a damage receiver
            var damageReceiver = other.GetComponentInParent<IDamageReceiver>();
            if (damageReceiver == null)
            {
                Debug.LogWarning($"[Projectile] No IDamageReceiver found on {other.gameObject.name} or parents");
            }
            else if (!damageReceiver.CanReceiveDamage)
            {
                Debug.LogWarning($"[Projectile] IDamageReceiver found but CanReceiveDamage=false on {other.gameObject.name}");
            }
            else
            {
                var damageInfo = new DamageInfo(
                    baseDamage: _damage,
                    type: _damageConfig.damageType,
                    shieldMultiplier: _damageConfig.shieldDamageMultiplier,
                    hullMultiplier: _damageConfig.hullDamageMultiplier,
                    source: _owner,
                    sourcePosition: hitPoint,
                    direction: _rigidbody.linearVelocity.normalized,
                    bypassesShield: _damageConfig.bypassesShield
                );

                Debug.Log($"[Projectile] Applying {_damage} {_damageConfig.damageType} damage to {other.gameObject.name}");

                var result = damageReceiver.ReceiveDamage(damageInfo);
            }

            if (_destroyOnHit)
            {
                // Spawn impact effect before destroying
                if (_impactConfig != null)
                {
                    ImpactEffect.Spawn(_impactConfig, hitPoint, _rigidbody.linearVelocity.normalized);
                }

                Destroy(gameObject);
            }
        }
    }
}
