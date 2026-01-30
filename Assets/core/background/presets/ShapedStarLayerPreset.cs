using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Background.Layers;

namespace Starfire.Core.Background.Presets
{
    /// <summary>
    /// ScriptableObject preset for ShapedStarLayer configuration.
    /// Assign to a ShapedStarLayer to override all its settings.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShapedStarPreset", menuName = "Starfire/Presets/Shaped Star Preset")]
    public class ShapedStarLayerPreset : ScriptableObject
    {
        [Header("Layer Settings")]
        [Tooltip("Parallax depth - lower values = farther/slower")]
        public float parallaxDepth = 0.1f;

        [Header("Shape Configuration")]
        [Tooltip("Up to 4 shapes with individual spawn weights and optional overrides")]
        public List<ShapeConfig> shapes = new List<ShapeConfig>
        {
            ShapeConfig.Default(StarShape.Circle, 0.4f),
            ShapeConfig.Default(StarShape.Diamond, 0.3f),
            ShapeConfig.Default(StarShape.FourPointStar, 0.2f),
            ShapeConfig.Default(StarShape.Square, 0.1f)
        };

        [Header("Star Field")]
        [Range(1, 100)]
        public float density = 20f;

        [Range(0, 1)]
        public float spawnChance = 0.8f;

        [Header("Shared Brightness")]
        [Range(0.1f, 2f)]
        public float brightnessMin = 0.3f;

        [Range(0.1f, 2f)]
        public float brightnessMax = 1f;

        [Tooltip("Distribution curve (0=favor dim, 0.5=uniform, 1=favor bright)")]
        [Range(0, 1)]
        public float brightnessDistribution = 0.5f;

        [Header("Shared Star Size")]
        [Min(0)]
        public float sizeMin = 0.02f;

        [Min(0)]
        public float sizeMax = 0.15f;

        [Range(0, 1)]
        public float sizeDistribution = 0.5f;

        [Header("Twinkle")]
        [Range(0, 5)]
        public float twinkleSpeed = 1f;

        [Range(0, 1)]
        public float twinkleAmount = 0.3f;

        [Header("Shared Color")]
        public Color starColor = Color.white;

        [Range(0, 1)]
        public float colorVariation = 0.3f;

        [Range(0, 1)]
        public float warmCoolMix = 0.5f;

        [Header("Shared Shape")]
        [Range(0, 1)]
        public float edgeSharpness = 0f;

        [Header("Background")]
        [Tooltip("Only the first layer should render background color; others should be transparent")]
        public bool renderBackground = false;
        public Color backgroundColor = new Color(0, 0, 0.02f, 1);

        [Header("Distribution")]
        [Tooltip("Random seed for this layer's star positions")]
        public int layerSeed = 0;

        [Tooltip("How much clustering affects star density (0 = uniform, 1 = heavily clustered)")]
        [Range(0f, 1f)]
        public float clusterAmount = 0.3f;

        [Tooltip("Scale of clusters (smaller = tighter clusters, larger = broader regions)")]
        [Min(0.01f)]
        public float clusterScale = 0.05f;

        [Header("Multi-Depth Configuration")]
        [Tooltip("When populated, renders multiple depth layers in a single draw call. Leave empty for single-depth mode.")]
        public List<ShapedDepthConfig> depths = new List<ShapedDepthConfig>();

        /// <summary>
        /// Apply all preset values to the given layer.
        /// </summary>
        public void ApplyTo(ShapedStarLayer layer)
        {
            layer.parallaxDepth = parallaxDepth;

            // Clear and copy shapes
            layer.shapes.Clear();
            foreach (var shape in shapes)
            {
                layer.shapes.Add(shape);
            }

            layer.density = density;
            layer.spawnChance = spawnChance;
            layer.brightnessMin = brightnessMin;
            layer.brightnessMax = brightnessMax;
            layer.brightnessDistribution = brightnessDistribution;
            layer.sizeMin = sizeMin;
            layer.sizeMax = sizeMax;
            layer.sizeDistribution = sizeDistribution;
            layer.twinkleSpeed = twinkleSpeed;
            layer.twinkleAmount = twinkleAmount;
            layer.starColor = starColor;
            layer.colorVariation = colorVariation;
            layer.warmCoolMix = warmCoolMix;
            layer.edgeSharpness = edgeSharpness;
            layer.renderBackground = renderBackground;
            layer.backgroundColor = backgroundColor;
            layer.layerSeed = layerSeed;
            layer.clusterAmount = clusterAmount;
            layer.clusterScale = clusterScale;

            // Clear and copy depths
            layer.depths.Clear();
            foreach (var depth in depths)
            {
                layer.depths.Add(depth);
            }
        }
    }
}
