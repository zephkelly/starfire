using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Raycast-backed projectile that performs a single raycast at fire time.
    /// A visual projectile travels to the predetermined hit point.
    /// Guarantees hit detection - no tunneling possible at any speed.
    /// </summary>
    public class RaycastProjectile : MonoBehaviour
    {
        private ProjectileSpawnContext _context;
        private ProjectileRaycastUtility.RaycastHitResult _hitResult;

        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private Vector2 _velocity;
        private float _travelTime;
        private float _elapsed;
        private bool _hasAppliedDamage;

        private SpriteRenderer _spriteRenderer;
        private TrailRenderer _trailRenderer;

        /// <summary>
        /// Initialize the raycast projectile with spawn context.
        /// Performs the raycast immediately to determine hit point.
        /// </summary>
        public void Initialize(ProjectileSpawnContext context)
        {
            _context = context;
            _startPosition = context.SpawnPosition;

            var config = context.ProjectileConfig;

            // Calculate effective speed including inherited velocity component
            float effectiveSpeed = config.speed;
            if (config.inheritVelocity && context.InheritedVelocity.sqrMagnitude > 0.01f)
            {
                // Add the component of inherited velocity in the firing direction
                effectiveSpeed += Vector2.Dot(context.InheritedVelocity, context.Direction);
            }

            // Clamp to positive speed
            effectiveSpeed = Mathf.Max(effectiveSpeed, 1f);

            // Calculate max distance based on speed and lifetime
            float maxDistance = effectiveSpeed * config.lifetime;

            // Store velocity for impact calculations
            _velocity = context.Direction * effectiveSpeed;

            // Perform the raycast at fire time
            bool bypassShields = context.DamageConfig?.bypassesShield ?? false;

            _hitResult = ProjectileRaycastUtility.Raycast(
                _startPosition,
                context.Direction,
                maxDistance,
                config.hitLayers,
                context.Owner,
                bypassShields
            );

            if (_hitResult.DidHit)
            {
                _endPosition = _hitResult.HitPoint;
                _travelTime = _hitResult.Distance / effectiveSpeed;
            }
            else
            {
                // No hit - travel for full lifetime then despawn
                _endPosition = _startPosition + context.Direction * maxDistance;
                _travelTime = config.lifetime;
            }

            // Set up visual representation
            SetupVisual();

            // Rotate to face direction of travel
            float angle = Mathf.Atan2(context.Direction.y, context.Direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void SetupVisual()
        {
            var config = _context.ProjectileConfig;

            // Try to copy visual from the projectile prefab if available
            if (_context.ProjectilePrefab != null)
            {
                var prefabSprite = _context.ProjectilePrefab.GetComponentInChildren<SpriteRenderer>();
                if (prefabSprite != null)
                {
                    _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    _spriteRenderer.sprite = prefabSprite.sprite;
                    _spriteRenderer.color = config.color;
                    _spriteRenderer.sortingOrder = prefabSprite.sortingOrder;
                    transform.localScale = Vector3.one * config.scale;
                }

                // Copy trail renderer if present
                var prefabTrail = _context.ProjectilePrefab.GetComponentInChildren<TrailRenderer>();
                if (prefabTrail != null)
                {
                    _trailRenderer = gameObject.AddComponent<TrailRenderer>();
                    _trailRenderer.time = prefabTrail.time;
                    _trailRenderer.startWidth = prefabTrail.startWidth * config.scale;
                    _trailRenderer.endWidth = prefabTrail.endWidth * config.scale;
                    _trailRenderer.material = prefabTrail.material;
                    _trailRenderer.colorGradient = prefabTrail.colorGradient;
                    _trailRenderer.sortingOrder = prefabTrail.sortingOrder;
                }
            }

            // Fallback: create a simple sprite if no prefab visual
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                _spriteRenderer.color = config.color;
                transform.localScale = Vector3.one * config.scale * 0.2f;

                // Create a simple circle sprite
                var texture = new Texture2D(32, 32);
                var pixels = new Color[32 * 32];
                Vector2 center = new Vector2(16, 16);
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        pixels[y * 32 + x] = dist <= 14 ? Color.white : Color.clear;
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                _spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // Calculate interpolation factor
            float t = _travelTime > 0 ? Mathf.Clamp01(_elapsed / _travelTime) : 1f;

            // Move visual along path
            transform.position = Vector2.Lerp(_startPosition, _endPosition, t);

            // Apply damage and effects when reaching target
            if (t >= 1f && !_hasAppliedDamage)
            {
                _hasAppliedDamage = true;

                if (_hitResult.DidHit)
                {
                    ProjectileRaycastUtility.ApplyDamage(
                        _hitResult,
                        _context.Damage,
                        _context.DamageConfig,
                        _context.Owner,
                        _context.Direction,
                        _context.ProjectileConfig.impactConfig,
                        _velocity
                    );
                }

                Destroy(gameObject);
            }
        }
    }
}
