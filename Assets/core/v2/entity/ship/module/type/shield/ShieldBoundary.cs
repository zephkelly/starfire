using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Creates an elliptical shield boundary collider that intercepts projectiles.
    /// Implements IV2DamageReceiver so V2Projectile's collision logic routes damage here.
    /// When shields are up, damage is absorbed. When depleted, the boundary disables itself
    /// and projectiles pass through to hit the hull.
    /// </summary>
    [RequireComponent(typeof(PolygonCollider2D))]
    public class ShieldBoundary : MonoBehaviour, IV2DamageReceiver
    {
        private const string ShieldLayerName = "Shield";

        private IShipShieldModule _shield;
        private PolygonCollider2D _collider;
        private IEntityController _ownerController;
        private ShieldVisual _shieldVisual;

        public IEntityController OwnerController => _ownerController;
        public ShieldVisual ShieldVisual => _shieldVisual;
        public IShipShieldModule ShieldModule => _shield;

        [Header("Debug")]
        [SerializeField] private Color _gizmoColorActive = new Color(0f, 0.8f, 1f, 0.5f);
        [SerializeField] private Color _gizmoColorDestroyed = new Color(1f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private bool _showGizmoAlways = false;

        public void Initialize(IShipShieldModule shield, IEntityController owner, ShieldVisual shieldVisual = null)
        {
            _shield = shield;
            _ownerController = owner;
            _shieldVisual = shieldVisual;

            _collider = GetComponent<PolygonCollider2D>();
            _collider.isTrigger = true;

            int shieldLayer = LayerMask.NameToLayer(ShieldLayerName);
            if (shieldLayer >= 0)
            {
                gameObject.layer = shieldLayer;
            }
            else
            {
                Debug.LogWarning("[ShieldBoundary] Shield layer not found. Configure a 'Shield' layer in Project Settings.");
            }

            UpdateColliderShape();

            if (_shield != null)
            {
                _shield.OnShieldDestroyed += OnShieldDestroyed;
                _shield.OnShieldRestored += OnShieldRestored;

                _collider.enabled = _shield.State != ShieldState.Destroyed;
            }
        }

        /// <summary>
        /// Called by V2Projectile when it collides with this shield boundary.
        /// V2Projectile finds this via GetComponentInParent&lt;IV2DamageReceiver&gt;.
        /// </summary>
        public void ReceiveDamage(V2DamageInfo damageInfo)
        {
            if (_shield == null || _shield.State == ShieldState.Destroyed)
            {
                return;
            }

            // Shield-bypassing damage passes through (projectile will continue to hull)
            var config = damageInfo.DamageConfig ?? V2WeaponDamageConfig.Default;
            if (config.bypassesShield)
            {
                return;
            }

            // Absorb damage through shield module
            _shield.AbsorbDamage(damageInfo);

            // Spawn shield impact effect with ship velocity
            if (_shield.ShieldImpactConfig != null)
            {
                Vector2 shipVelocity = _ownerController.Rigid2D != null
                    ? _ownerController.Rigid2D.linearVelocity
                    : Vector2.zero;

                Vector2 hitPoint = damageInfo.HitPoint;
                Vector2 normal = CalculateNormalAtPoint(hitPoint);

                ShieldImpactEffect.Spawn(_shield.ShieldImpactConfig, hitPoint, normal, shipVelocity);
            }

            // Notify shield visual for ripple effect
            if (_shieldVisual != null)
            {
                _shieldVisual.RegisterImpact(damageInfo.HitPoint);
            }
        }

        /// <summary>
        /// Calculate the outward-facing normal at a world-space point on the ellipse boundary.
        /// </summary>
        private Vector2 CalculateNormalAtPoint(Vector2 worldPoint)
        {
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            Vector2 relative = localPoint - _shield.BoundaryOffset;
            Vector2 size = _shield.BoundarySize;

            // Gradient of ellipse equation: (2x/a², 2y/b²)
            Vector2 localNormal = new Vector2(
                relative.x / (size.x * size.x),
                relative.y / (size.y * size.y)
            ).normalized;

            return ((Vector2)transform.TransformDirection(localNormal)).normalized;
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
            }
        }

        private void OnShieldRestored()
        {
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }

        private void OnDestroy()
        {
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
            Vector2 size = _shield?.BoundarySize ?? new Vector2(2f, 1.5f);
            Vector2 offset = _shield?.BoundaryOffset ?? Vector2.zero;
            int resolution = _shield?.BoundaryResolution ?? 24;

            bool isDestroyed = _shield?.State == ShieldState.Destroyed;
            Gizmos.color = isDestroyed ? _gizmoColorDestroyed : _gizmoColorActive;

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

            Gizmos.color = isDestroyed ? Color.red : Color.cyan;
            Vector3 centerWorld = transform.TransformPoint(new Vector3(offset.x, offset.y, 0f));
            float markerSize = 0.1f;
            Gizmos.DrawLine(centerWorld - Vector3.right * markerSize, centerWorld + Vector3.right * markerSize);
            Gizmos.DrawLine(centerWorld - Vector3.up * markerSize, centerWorld + Vector3.up * markerSize);
        }
    }
}
