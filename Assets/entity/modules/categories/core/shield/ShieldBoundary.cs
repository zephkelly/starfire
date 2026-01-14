using Starfire.Core;
using Starfire.Entity.Modules.Damage;
using Starfire.Entity.Modules.Weapon;
using UnityEngine;

namespace Starfire.Entity.Modules.Shield
{
    /// <summary>
    /// Creates an elliptical shield boundary collider that intercepts projectiles.
    /// When shields are up, projectiles hit this boundary and are destroyed.
    /// When shields are depleted, the boundary is disabled and projectiles pass through.
    /// </summary>
    [RequireComponent(typeof(PolygonCollider2D))]
    public class ShieldBoundary : MonoBehaviour
    {
        private IShieldModule _shield;
        private PolygonCollider2D _collider;
        private EntityControllerBase _ownerController;
        private IDamageReceiver _damageReceiver;
        private ShieldVisual _shieldVisual;

        [Header("Debug")]
        [SerializeField] private Color _gizmoColorActive = new Color(0f, 0.8f, 1f, 0.5f);
        [SerializeField] private Color _gizmoColorDestroyed = new Color(1f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private bool _showGizmoAlways = false;

        /// <summary>
        /// Initialize the shield boundary with the shield module data.
        /// </summary>
        public void Initialize(IShieldModule shield, EntityControllerBase owner, ShieldVisual shieldVisual = null)
        {
            _shield = shield;
            _ownerController = owner;
            _damageReceiver = owner.GetComponent<IDamageReceiver>();
            _shieldVisual = shieldVisual;

            _collider = GetComponent<PolygonCollider2D>();
            _collider.isTrigger = true;

            // Set to Shield layer
            int shieldLayer = LayerMask.NameToLayer(GameLayers.Shield);
            if (shieldLayer >= 0)
            {
                gameObject.layer = shieldLayer;
            }
            else
            {
                Debug.LogWarning("[ShieldBoundary] Shield layer not found. Configure Layer 11 as 'Shield' in Project Settings.");
            }

            // Generate ellipse collider shape
            UpdateColliderShape();

            // Subscribe to shield events
            if (_shield != null)
            {
                _shield.OnShieldDestroyed += OnShieldDestroyed;
                _shield.OnShieldRestored += OnShieldRestored;

                // Set initial state based on current shield status
                _collider.enabled = _shield.State != ShieldState.Destroyed;
            }
        }

        private void UpdateColliderShape()
        {
            if (_shield == null || _collider == null) return;

            Vector2[] points = GenerateEllipsePoints(
                _shield.BoundarySize,
                _shield.BoundaryOffset,
                _shield.BoundaryResolution
            );

            _collider.pathCount = 1;
            _collider.SetPath(0, points);
        }

        private Vector2[] GenerateEllipsePoints(Vector2 size, Vector2 offset, int resolution)
        {
            Vector2[] points = new Vector2[resolution];

            for (int i = 0; i < resolution; i++)
            {
                float angle = (i / (float)resolution) * Mathf.PI * 2f;
                points[i] = new Vector2(
                    Mathf.Cos(angle) * size.x + offset.x,
                    Mathf.Sin(angle) * size.y + offset.y
                );
            }

            return points;
        }

        private void OnShieldDestroyed()
        {
            if (_collider != null)
            {
                _collider.enabled = false;
                Debug.Log($"[ShieldBoundary] {_ownerController?.name} shields destroyed - boundary disabled");
            }
        }

        private void OnShieldRestored()
        {
            if (_collider != null)
            {
                _collider.enabled = true;
                Debug.Log($"[ShieldBoundary] {_ownerController?.name} shields restored - boundary enabled");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only process if shields are active
            if (_shield == null || _shield.State == ShieldState.Destroyed)
            {
                return;
            }

            // Check for projectile component
            var projectile = other.GetComponent<Projectile>();
            if (projectile == null)
            {
                return;
            }

            // Don't block projectiles from the same owner
            if (projectile.Owner == _ownerController)
            {
                return;
            }

            // Shield-bypassing projectiles pass through to hit hull directly
            if (projectile.DamageConfig != null && projectile.DamageConfig.bypassesShield)
            {
                return;
            }

            // Calculate hit point using ray-ellipse intersection
            // Trace backward along projectile velocity to find actual entry point
            // This works correctly even at extreme speeds where the projectile has passed through the shield
            Vector2 hitPoint;
            Vector2 normal;

            var projectileRb = other.GetComponent<Rigidbody2D>();
            Vector2 velocity = projectileRb != null ? projectileRb.linearVelocity : Vector2.zero;
            Vector2 projectileWorldPos = other.transform.position;

            Vector2 ellipseCenter = _shield.BoundaryOffset; // Local space
            Vector2 ellipseSize = _shield.BoundarySize;     // Semi-axes (a, b)

            if (velocity.sqrMagnitude > 0.01f)
            {
                // Convert to local space for intersection math
                Vector2 projectileLocalPos = transform.InverseTransformPoint(projectileWorldPos);
                Vector2 localVelocity = transform.InverseTransformDirection(velocity);

                // Ray from projectile position, going backward (reverse of velocity)
                Vector2 rayOrigin = projectileLocalPos - ellipseCenter; // Relative to ellipse center
                Vector2 rayDir = -localVelocity.normalized;

                // Solve ray-ellipse intersection: ((Ox + t*Dx)/a)² + ((Oy + t*Dy)/b)² = 1
                // This is a quadratic equation: At² + Bt + C = 0
                float a = ellipseSize.x;
                float b = ellipseSize.y;

                float A = (rayDir.x * rayDir.x) / (a * a) + (rayDir.y * rayDir.y) / (b * b);
                float B = 2f * ((rayOrigin.x * rayDir.x) / (a * a) + (rayOrigin.y * rayDir.y) / (b * b));
                float C = (rayOrigin.x * rayOrigin.x) / (a * a) + (rayOrigin.y * rayOrigin.y) / (b * b) - 1f;

                float discriminant = B * B - 4f * A * C;

                if (discriminant >= 0f)
                {
                    float sqrtDisc = Mathf.Sqrt(discriminant);
                    float t1 = (-B - sqrtDisc) / (2f * A);
                    float t2 = (-B + sqrtDisc) / (2f * A);

                    // We want the smallest positive t (first intersection going backward)
                    float t = (t1 > 0f) ? t1 : t2;

                    if (t > 0f)
                    {
                        // Calculate intersection point in local space
                        Vector2 localHitPoint = rayOrigin + rayDir * t + ellipseCenter;
                        hitPoint = transform.TransformPoint(localHitPoint);

                        // Normal at ellipse point: gradient of (x/a)² + (y/b)² = 1
                        // ∇ = (2x/a², 2y/b²), normalized
                        Vector2 hitRelative = localHitPoint - ellipseCenter;
                        Vector2 localNormal = new Vector2(hitRelative.x / (a * a), hitRelative.y / (b * b)).normalized;
                        normal = ((Vector2)transform.TransformDirection(localNormal)).normalized;
                    }
                    else
                    {
                        // Fallback: project from center (shouldn't happen normally)
                        CalculateFallbackHitPoint(projectileLocalPos, ellipseCenter, ellipseSize, out hitPoint, out normal);
                    }
                }
                else
                {
                    // No intersection (shouldn't happen if trigger fired)
                    Vector2 projectileLocalPos2 = transform.InverseTransformPoint(projectileWorldPos);
                    CalculateFallbackHitPoint(projectileLocalPos2, ellipseCenter, ellipseSize, out hitPoint, out normal);
                }
            }
            else
            {
                // No velocity - fall back to projection from center
                Vector2 projectileLocalPos = transform.InverseTransformPoint(projectileWorldPos);
                CalculateFallbackHitPoint(projectileLocalPos, ellipseCenter, ellipseSize, out hitPoint, out normal);
            }

            // Apply damage through the damage receiver using projectile's full config
            if (_damageReceiver != null && _damageReceiver.CanReceiveDamage)
            {
                var config = projectile.DamageConfig ?? WeaponDamageConfig.Default;

                var damageInfo = new DamageInfo(
                    baseDamage: projectile.Damage,
                    type: config.damageType,
                    shieldMultiplier: config.shieldDamageMultiplier,
                    hullMultiplier: config.hullDamageMultiplier,
                    source: projectile.Owner,
                    sourcePosition: projectile.transform.position,
                    direction: normal,
                    bypassesShield: false,
                    shieldPenetration: config.shieldPenetration
                );

                Debug.Log($"[ShieldBoundary] Applying {projectile.Damage} {config.damageType} damage to shield of {_ownerController?.name} (penetration: {config.shieldPenetration:P0})");

                var result = _damageReceiver.ReceiveDamage(damageInfo);
            }

            // Spawn shield impact effect with ship velocity so it moves with the ship
            if (_shield.ShieldImpactConfig != null)
            {
                Vector2 shipVelocity = _ownerController?.Rigidbody?.linearVelocity ?? Vector2.zero;
                ShieldImpactEffect.Spawn(_shield.ShieldImpactConfig, hitPoint, normal, shipVelocity);
            }

            // Spawn projectile's impact effect at the shield surface with reflection particles
            var projectileImpactConfig = projectile.ImpactConfig;
            if (projectileImpactConfig != null)
            {
                // Use overload with velocity and normal for shield reflection particles
                // (reuse velocity from earlier - already fetched from projectileRb)
                ImpactEffect.Spawn(projectileImpactConfig, hitPoint, -normal, velocity, normal);
            }

            // Notify shield visual for ripple effect
            if (_shieldVisual != null)
            {
                _shieldVisual.RegisterImpact(hitPoint);
            }

            // Mark projectile as consumed to prevent double-processing
            // This prevents queued collision events from applying damage again
            projectile.IsConsumed = true;

            // Destroy the projectile
            Destroy(other.gameObject);
        }

        /// <summary>
        /// Fallback hit point calculation when velocity-based ray intersection isn't available.
        /// Projects from ellipse center through projectile position to find boundary point.
        /// </summary>
        private void CalculateFallbackHitPoint(Vector2 projectileLocalPos, Vector2 ellipseCenter,
            Vector2 ellipseSize, out Vector2 hitPoint, out Vector2 normal)
        {
            Vector2 toProjectile = projectileLocalPos - ellipseCenter;
            Vector2 normalizedDir = new Vector2(toProjectile.x / ellipseSize.x, toProjectile.y / ellipseSize.y);
            float normalizedDist = normalizedDir.magnitude;

            if (normalizedDist > 0.001f)
            {
                Vector2 unitDir = normalizedDir / normalizedDist;
                Vector2 localHitPoint = ellipseCenter + new Vector2(unitDir.x * ellipseSize.x, unitDir.y * ellipseSize.y);
                hitPoint = transform.TransformPoint(localHitPoint);
                Vector2 worldCenter = transform.TransformPoint(ellipseCenter);
                normal = (hitPoint - worldCenter).normalized;
            }
            else
            {
                // Projectile exactly at center - use top of ellipse as fallback
                hitPoint = transform.TransformPoint(ellipseCenter + Vector2.up * ellipseSize.y);
                normal = Vector2.up;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_shield != null)
            {
                _shield.OnShieldDestroyed -= OnShieldDestroyed;
                _shield.OnShieldRestored -= OnShieldRestored;
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawBoundaryGizmo();
        }

        private void OnDrawGizmos()
        {
            if (_showGizmoAlways)
            {
                DrawBoundaryGizmo();
            }
        }

        private void DrawBoundaryGizmo()
        {
            // Determine size and offset from shield or default values
            Vector2 size = _shield?.BoundarySize ?? new Vector2(2f, 1.5f);
            Vector2 offset = _shield?.BoundaryOffset ?? Vector2.zero;
            int resolution = _shield?.BoundaryResolution ?? 24;

            // Determine color based on shield state
            bool isDestroyed = _shield?.State == ShieldState.Destroyed;
            Gizmos.color = isDestroyed ? _gizmoColorDestroyed : _gizmoColorActive;

            // Draw ellipse
            int segments = Mathf.Max(resolution, 32);
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 localPoint = new Vector3(
                    Mathf.Cos(angle) * size.x + offset.x,
                    Mathf.Sin(angle) * size.y + offset.y,
                    0f
                );
                Vector3 worldPoint = transform.TransformPoint(localPoint);

                if (i > 0)
                {
                    Gizmos.DrawLine(prevPoint, worldPoint);
                }
                prevPoint = worldPoint;
            }

            // Draw center marker
            Gizmos.color = isDestroyed ? Color.red : Color.cyan;
            Vector3 centerWorld = transform.TransformPoint(new Vector3(offset.x, offset.y, 0f));
            float markerSize = 0.1f;
            Gizmos.DrawLine(centerWorld - Vector3.right * markerSize, centerWorld + Vector3.right * markerSize);
            Gizmos.DrawLine(centerWorld - Vector3.up * markerSize, centerWorld + Vector3.up * markerSize);
        }
    }
}
