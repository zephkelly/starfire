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
            // Expand the frustum by the region radius to check for intersection
            var expandedRect = new Rect(
                frustumRect.x - Radius,
                frustumRect.y - Radius,
                frustumRect.width + Radius * 2,
                frustumRect.height + Radius * 2
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
                    return 1f - Mathf.InverseLerp(falloffStart, Radius, distance);

                case NebulaEdgeBehavior.SharpBoundary:
                    return distance <= Radius ? 1f : 0f;

                case NebulaEdgeBehavior.InverseFalloff:
                    if (distance >= Radius + Config.falloffDistance) return 1f;
                    if (distance <= Radius) return 0f;
                    return Mathf.InverseLerp(Radius, Radius + Config.falloffDistance, distance);

                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Updates the material's region-specific shader properties.
        /// </summary>
        internal void UpdateMaterialProperties()
        {
            if (Material == null) return;

            Material.SetVector(RegionCenterID, new Vector4(WorldPosition.x, WorldPosition.y, 0, 0));
            Material.SetFloat(RegionRadiusID, Radius);
            Material.SetFloat(RegionFalloffID, Config.falloffDistance);
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
