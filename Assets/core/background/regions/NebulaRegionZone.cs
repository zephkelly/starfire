using UnityEngine;

namespace Starfire.Core.Background.Regions
{
    /// <summary>
    /// Scene-placeable component for defining nebula regions.
    /// Place this on an empty GameObject to create a nebula region at that location.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Starfire/Background/Nebula Region Zone")]
    public class NebulaRegionZone : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Configuration defining the nebula appearance and behavior")]
        [SerializeField] private NebulaRegionConfig config;

        [Tooltip("Radius of the region in world units")]
        [Min(1f)]
        [SerializeField] private float radius = 50f;

        [Header("Runtime Override")]
        [Tooltip("Override the config's default radius with this component's radius")]
        [SerializeField] private bool useLocalRadius = true;

        [Header("Debug Visualization")]
        [Tooltip("Color for the gizmo visualization")]
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.6f, 1f, 0.5f);

        [Tooltip("Show the region boundary in Scene view")]
        [SerializeField] private bool showGizmo = true;

        // Runtime region reference
        private NebulaRegion _runtimeRegion;

        /// <summary>
        /// The configuration for this region.
        /// </summary>
        public NebulaRegionConfig Config => config;

        /// <summary>
        /// The radius of this region.
        /// </summary>
        public float Radius
        {
            get => useLocalRadius ? radius : (config != null ? config.defaultRadius : radius);
            set
            {
                radius = value;
                useLocalRadius = true;
                if (_runtimeRegion != null)
                {
                    _runtimeRegion.Radius = radius;
                    _runtimeRegion.MarkDirty();
                }
            }
        }

        /// <summary>
        /// The runtime region created by this zone.
        /// </summary>
        public NebulaRegion RuntimeRegion => _runtimeRegion;

        #region Unity Lifecycle

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void Update()
        {
            // Sync position changes to runtime region
            if (_runtimeRegion != null)
            {
                Vector2 currentPos = (Vector2)transform.position;
                if (_runtimeRegion.WorldPosition != currentPos)
                {
                    _runtimeRegion.WorldPosition = currentPos;
                    _runtimeRegion.MarkDirty();
                }

                float currentRadius = Radius;
                if (Mathf.Abs(_runtimeRegion.Radius - currentRadius) > 0.001f)
                {
                    _runtimeRegion.Radius = currentRadius;
                    _runtimeRegion.MarkDirty();
                }
            }
        }

        private void OnValidate()
        {
            // Clamp radius
            radius = Mathf.Max(1f, radius);

            // Update runtime region if it exists
            if (_runtimeRegion != null)
            {
                _runtimeRegion.WorldPosition = (Vector2)transform.position;
                _runtimeRegion.Radius = Radius;
                _runtimeRegion.MarkDirty();
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;
            DrawRegionGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawRegionGizmo(true);
        }

        #endregion

        #region Registration

        private void Register()
        {
            if (config == null)
            {
                Debug.LogWarning($"NebulaRegionZone '{name}' has no config assigned.", this);
                return;
            }

            var manager = NebulaRegionManager.Instance;
            if (manager != null)
            {
                manager.RegisterZone(this);
            }
        }

        private void Unregister()
        {
            var manager = NebulaRegionManager.Instance;
            if (manager != null)
            {
                manager.UnregisterZone(this);
            }
        }

        internal void SetRuntimeRegion(NebulaRegion region)
        {
            _runtimeRegion = region;
        }

        #endregion

        #region Gizmo Drawing

        private void DrawRegionGizmo(bool selected)
        {
            Vector3 center = transform.position;
            float currentRadius = Radius;

            // Determine color based on edge behavior
            Color baseColor = gizmoColor;
            if (config != null)
            {
                baseColor = config.edgeBehavior switch
                {
                    NebulaEdgeBehavior.SmoothFalloff => new Color(0.2f, 0.6f, 1f, 0.5f),
                    NebulaEdgeBehavior.SharpBoundary => new Color(1f, 0.4f, 0.2f, 0.5f),
                    NebulaEdgeBehavior.InverseFalloff => new Color(0.8f, 0.2f, 0.8f, 0.5f),
                    _ => gizmoColor
                };
            }

            // Main boundary
            Gizmos.color = selected ? baseColor : baseColor * 0.7f;
            DrawCircle(center, currentRadius, 64);

            // Fill disc
            if (selected)
            {
                Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.1f);
                DrawFilledCircle(center, currentRadius, 32);
            }

            // Falloff boundary (for smooth/inverse)
            if (config != null)
            {
                if (config.edgeBehavior == NebulaEdgeBehavior.SmoothFalloff)
                {
                    float innerRadius = currentRadius - config.falloffDistance;
                    if (innerRadius > 0)
                    {
                        Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
                        DrawCircle(center, innerRadius, 32);

                        // Draw radial lines to show falloff
                        if (selected)
                        {
                            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f);
                            for (int i = 0; i < 8; i++)
                            {
                                float angle = i * 45f * Mathf.Deg2Rad;
                                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                                Gizmos.DrawLine(center + dir * innerRadius, center + dir * currentRadius);
                            }
                        }
                    }
                }
                else if (config.edgeBehavior == NebulaEdgeBehavior.InverseFalloff)
                {
                    float outerRadius = currentRadius + config.falloffDistance;
                    Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
                    DrawCircle(center, outerRadius, 32);

                    // Draw X pattern to indicate "clear zone"
                    if (selected)
                    {
                        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
                        float crossSize = currentRadius * 0.3f;
                        Gizmos.DrawLine(center + new Vector3(-crossSize, -crossSize, 0),
                                       center + new Vector3(crossSize, crossSize, 0));
                        Gizmos.DrawLine(center + new Vector3(-crossSize, crossSize, 0),
                                       center + new Vector3(crossSize, -crossSize, 0));
                    }
                }
            }

            // Draw center point
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(center, currentRadius * 0.02f);

            // Label
#if UNITY_EDITOR
            if (selected)
            {
                string label = config != null ? config.name : "No Config";
                UnityEditor.Handles.Label(center + Vector3.up * (currentRadius + 2f), label);
            }
#endif
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }

        private void DrawFilledCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);

                // Draw triangle from center to edge
                Gizmos.DrawLine(center, p1);
                Gizmos.DrawLine(p1, p2);
            }
        }

        #endregion
    }
}
