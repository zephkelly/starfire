using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Background.Layers;

namespace Starfire.Core.Background.Presets
{
    /// <summary>
    /// ScriptableObject preset for MultiStarLayer configuration.
    /// Assign to a MultiStarLayer to override all its settings.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMultiStarPreset", menuName = "Starfire/Presets/Multi Star Preset")]
    public class MultiStarLayerPreset : ScriptableObject
    {
        [Header("Layer Settings")]
        [Tooltip("Parallax depth - lower values = farther/slower")]
        public float parallaxDepth = 0.1f;

        [Header("Multi-Depth Configuration")]
        [Tooltip("Configure each depth level. Supports up to 8 depths in a single draw call.")]
        public List<MultiStarLayer.DepthConfig> depths = new List<MultiStarLayer.DepthConfig>
        {
            MultiStarLayer.DepthConfig.Default(0),
            MultiStarLayer.DepthConfig.Default(1),
            MultiStarLayer.DepthConfig.Default(2),
            MultiStarLayer.DepthConfig.Default(3)
        };

        [Header("Shared Star Settings")]
        [Range(0, 1)]
        [Tooltip("Base spawn chance for all depths")]
        public float spawnChance = 0.8f;

        [Header("Brightness")]
        [Range(0.1f, 2f)]
        public float brightnessMin = 0.3f;

        [Range(0.1f, 2f)]
        public float brightnessMax = 1f;

        [Tooltip("Distribution curve (0=favor dim, 0.5=uniform, 1=favor bright)")]
        [Range(0, 1)]
        public float brightnessDistribution = 0.5f;

        [Header("Twinkle")]
        [Range(0, 5)]
        public float twinkleSpeed = 1f;

        [Range(0, 1)]
        public float twinkleAmount = 0.3f;

        [Header("Color")]
        [Range(0, 1)]
        public float colorVariation = 0.3f;

        [Range(0, 1)]
        public float warmCoolMix = 0.5f;

        [Header("Shape")]
        [Range(0, 1)]
        public float edgeSharpness = 0f;

        [Range(0, 1)]
        public float sizeDistribution = 0.5f;

        [Header("Background")]
        [Tooltip("Only the first layer should render background color")]
        public bool renderBackground = true;
        public Color backgroundColor = new Color(0, 0, 0.02f, 1);

        [Header("Clustering")]
        [Tooltip("How much clustering affects star density (0 = uniform, 1 = heavily clustered)")]
        [Range(0f, 1f)]
        public float clusterAmount = 0.3f;

        [Tooltip("Scale of clusters (smaller = tighter clusters, larger = broader regions)")]
        [Min(0.01f)]
        public float clusterScale = 0.05f;

        /// <summary>
        /// Apply all preset values to the given layer.
        /// </summary>
        public void ApplyTo(MultiStarLayer layer)
        {
            layer.parallaxDepth = parallaxDepth;

            // Clear and copy depths
            layer.depths.Clear();
            foreach (var depth in depths)
            {
                layer.depths.Add(depth);
            }

            layer.spawnChance = spawnChance;
            layer.brightnessMin = brightnessMin;
            layer.brightnessMax = brightnessMax;
            layer.brightnessDistribution = brightnessDistribution;
            layer.twinkleSpeed = twinkleSpeed;
            layer.twinkleAmount = twinkleAmount;
            layer.colorVariation = colorVariation;
            layer.warmCoolMix = warmCoolMix;
            layer.edgeSharpness = edgeSharpness;
            layer.sizeDistribution = sizeDistribution;
            layer.renderBackground = renderBackground;
            layer.backgroundColor = backgroundColor;
            layer.clusterAmount = clusterAmount;
            layer.clusterScale = clusterScale;
        }
    }
}
