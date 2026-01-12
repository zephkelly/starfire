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

        public EntityControllerBase Owner => _owner;
        public float Damage => _damage;

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
            Vector2 inheritedVelocity = default)
        {
            _owner = owner;
            _damage = damage;
            _lifetime = lifetime;
            _destroyOnHit = destroyOnHit;
            _hitLayers = hitLayers;
            _spawnTime = Time.time;

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
            // Check if we should interact with this layer
            if (_hitLayers != 0 && (_hitLayers & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            // Don't hit owner
            var otherController = other.GetComponentInParent<EntityControllerBase>();
            if (otherController != null && otherController == _owner)
            {
                return;
            }

            // Apply damage if target has a damage receiver
            // TODO: Implement damage system integration
            // var damageReceiver = other.GetComponent<IDamageReceiver>();
            // damageReceiver?.TakeDamage(_damage, _owner);

            if (_destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
}
