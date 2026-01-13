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

        [Header("Debug")]
        [SerializeField] private Color _gizmoColorActive = new Color(0f, 0.8f, 1f, 0.5f);
        [SerializeField] private Color _gizmoColorDestroyed = new Color(1f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private bool _showGizmoAlways = false;

        /// <summary>
        /// Initialize the shield boundary with the shield module data.
        /// </summary>
        public void Initialize(IShieldModule shield, EntityControllerBase owner)
        {
            _shield = shield;
            _ownerController = owner;
            _damageReceiver = owner.GetComponent<IDamageReceiver>();

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

            Debug.Log($"[ShieldBoundary] {_ownerController?.name} shield hit by projectile from {projectile.Owner?.name}");

            // Apply damage through the damage receiver
            if (_damageReceiver != null && _damageReceiver.CanReceiveDamage)
            {
                Vector2 hitPoint = other.ClosestPoint(transform.position);
                Vector2 direction = (hitPoint - (Vector2)transform.position).normalized;

                var damageInfo = new DamageInfo(
                    baseDamage: projectile.Damage,
                    type: DamageType.Energy, // Default, could be extended to get from projectile
                    shieldMultiplier: 1f,
                    hullMultiplier: 1f,
                    source: projectile.Owner,
                    sourcePosition: projectile.transform.position,
                    direction: direction,
                    bypassesShield: false
                );

                var result = _damageReceiver.ReceiveDamage(damageInfo);
                Debug.Log($"[ShieldBoundary] Damage result: Shield={result.ShieldDamageDealt}, Hull={result.HullDamageDealt}");
            }

            // Spawn shield impact effect
            if (_shield.ShieldImpactConfig != null)
            {
                Vector2 hitPoint = other.ClosestPoint(transform.position);
                Vector2 normal = (hitPoint - (Vector2)transform.position).normalized;
                ShieldImpactEffect.Spawn(_shield.ShieldImpactConfig, hitPoint, normal);
            }

            // Destroy the projectile
            Destroy(other.gameObject);
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
