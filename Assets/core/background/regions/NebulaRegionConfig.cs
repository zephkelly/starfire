using UnityEngine;
using Starfire.Core.Background.Presets;

namespace Starfire.Core.Background.Regions
{
    /// <summary>
    /// ScriptableObject configuration for a nebula region.
    /// Defines the visual appearance and behavior of nebula regions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNebulaRegionConfig", menuName = "Starfire/Nebula/Region Config")]
    public class NebulaRegionConfig : ScriptableObject
    {
        [Header("Edge Behavior")]
        [Tooltip("How the region's edges are rendered")]
        public NebulaEdgeBehavior edgeBehavior = NebulaEdgeBehavior.SmoothFalloff;

        [Tooltip("Default radius for regions using this config")]
        [Min(1f)]
        public float defaultRadius = 100f;

        [Tooltip("Distance over which the edge fades (for smooth falloff)")]
        [Min(0f)]
        public float falloffDistance = 20f;

        [Header("Nebula Type")]
        [Tooltip("Use the stylized nebula shader instead of the basic one")]
        public bool useStylizedNebula = false;

        [Header("Visual Settings")]
        [Tooltip("Preset for basic nebula appearance (used when useStylizedNebula is false)")]
        public NebulaLayerPreset nebulaPreset;

        [Tooltip("Preset for stylized nebula appearance (used when useStylizedNebula is true)")]
        public StylizedNebulaLayerPreset stylizedPreset;

        [Header("Rendering")]
        [Tooltip("Parallax depth for this region (lower = farther/slower)")]
        [Range(0.001f, 0.1f)]
        public float parallaxDepth = 0.02f;

        [Tooltip("Sorting priority when multiple regions overlap (higher = renders on top)")]
        public int sortingPriority = 0;

        /// <summary>
        /// Gets the appropriate preset based on the nebula type setting.
        /// </summary>
        public ScriptableObject ActivePreset => useStylizedNebula ? (ScriptableObject)stylizedPreset : nebulaPreset;
    }
}
