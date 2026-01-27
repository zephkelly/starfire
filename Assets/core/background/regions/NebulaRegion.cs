using System;
using UnityEngine;

namespace Starfire.Core.Background.Regions
{
    /// <summary>
    /// Runtime representation of an active nebula region.
    /// Manages its own material and rendering state.
    /// </summary>
    public class NebulaRegion
    {
        private static int _nextId = 0;

        /// <summary>
        /// Unique identifier for this region.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// World-space position of the region center.
        /// </summary>
        public Vector2 WorldPosition { get; set; }

        /// <summary>
        /// Radius of the region in world units.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// Configuration defining the region's appearance and behavior.
        /// </summary>
        public NebulaRegionConfig Config { get; }

        /// <summary>
        /// Whether this region is currently active and should be rendered.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // Internal rendering state
        internal Material Material { get; set; }
        internal MeshRenderer Renderer { get; set; }
        internal GameObject QuadObject { get; set; }
        internal bool IsDirty { get; set; } = true;

        // Shader property IDs (cached for performance)
        private static readonly int RegionCenterID = Shader.PropertyToID("_RegionCenter");
        private static readonly int RegionRadiusID = Shader.PropertyToID("_RegionRadius");
        private static readonly int RegionFalloffID = Shader.PropertyToID("_RegionFalloff");
        private static readonly int RegionFalloffPowerID = Shader.PropertyToID("_RegionFalloffPower");
        private static readonly int RegionEdgeModeID = Shader.PropertyToID("_RegionEdgeMode");
        private static readonly int ParallaxFactorID = Shader.PropertyToID("_ParallaxFactor");

        /// <summary>
        /// Creates a new nebula region with the specified parameters.
        /// </summary>
        public NebulaRegion(Vector2 position, float radius, NebulaRegionConfig config)
        {
            Id = $"NebulaRegion_{_nextId++}";
            WorldPosition = position;
            Radius = radius;
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Checks if a point is within this region's bounds.
        /// </summary>
        public bool ContainsPoint(Vector2 point)
        {
            return Vector2.Distance(point, WorldPosition) <= Radius;
        }

        /// <summary>
        /// Checks if this region intersects with a camera frustum rect.
        /// </summary>
        public bool IntersectsFrustum(Rect frustumRect)
        {
            // For InverseFalloff, the nebula extends BEYOND the radius by falloffDistance
            // For other modes, the nebula is contained within the radius
            float effectiveRadius = Config.edgeBehavior == NebulaEdgeBehavior.InverseFalloff
                ? Radius + Config.falloffDistance
                : Radius;

            // Expand the frustum by the effective radius to check for intersection
            var expandedRect = new Rect(
                frustumRect.x - effectiveRadius,
                frustumRect.y - effectiveRadius,
                frustumRect.width + effectiveRadius * 2,
                frustumRect.height + effectiveRadius * 2
            );

            return expandedRect.Contains(WorldPosition);
        }

        /// <summary>
        /// Gets the density multiplier at a specific point based on edge behavior.
        /// </summary>
        public float GetDensityAtPoint(Vector2 point)
        {
            float distance = Vector2.Distance(point, WorldPosition);

            switch (Config.edgeBehavior)
            {
                case NebulaEdgeBehavior.SmoothFalloff:
                    float falloffStart = Radius - Config.falloffDistance;
                    if (distance <= falloffStart) return 1f;
                    if (distance >= Radius) return 0f;
                    float t = Mathf.InverseLerp(falloffStart, Radius, distance);
                    // Apply smoothstep-like curve then power
                    t = t * t * (3f - 2f * t); // smoothstep
                    return 1f - Mathf.Pow(t, Config.falloffPower);

                case NebulaEdgeBehavior.SharpBoundary:
                    return distance <= Radius ? 1f : 0f;

                case NebulaEdgeBehavior.InverseFalloff:
                    if (distance >= Radius + Config.falloffDistance) return 1f;
                    if (distance <= Radius) return 0f;
                    float tInv = Mathf.InverseLerp(Radius, Radius + Config.falloffDistance, distance);
                    // Apply smoothstep-like curve then power
                    tInv = tInv * tInv * (3f - 2f * tInv); // smoothstep
                    return Mathf.Pow(tInv, Config.falloffPower);

                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Updates the material's region-specific shader properties.
        /// Uses virtual coordinates for floating origin compatibility.
        /// </summary>
        internal void UpdateMaterialProperties()
        {
            if (Material == null) return;

            // Convert to virtual space for floating origin compatibility
            // This ensures _RegionCenter matches the coordinate space used by _CameraWorldPos
            var manager = NebulaRegionManager.Instance;
            Vector2 virtualCenter = manager != null
                ? manager.GetVirtualPosition(WorldPosition)
                : WorldPosition;

            Material.SetVector(RegionCenterID, new Vector4(virtualCenter.x, virtualCenter.y, 0, 0));
            Material.SetFloat(RegionRadiusID, Radius);
            Material.SetFloat(RegionFalloffID, Config.falloffDistance);
            Material.SetFloat(RegionFalloffPowerID, Config.falloffPower);
            Material.SetFloat(RegionEdgeModeID, (float)Config.edgeBehavior);
            Material.SetFloat(ParallaxFactorID, Config.parallaxDepth);

            IsDirty = false;
        }

        /// <summary>
        /// Marks this region as needing a material property update.
        /// </summary>
        public void MarkDirty()
        {
            IsDirty = true;
        }
    }
}
