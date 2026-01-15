using System.Collections.Generic;
using UnityEngine;
using Starfire.Core;
using Starfire.Core.Background.Presets;

namespace Starfire.Core.Background.Layers
{
    /// <summary>
    /// A high-performance layer that renders multiple star depths in a single draw call.
    /// Use this instead of multiple StarLayer instances for better GPU performance.
    /// Supports up to 8 depth levels in a single pass.
    /// </summary>
    [System.Serializable]
    public class MultiStarLayer : StarfieldLayer
    {
        [Header("Preset")]
        [Tooltip("Optional preset to override all settings")]
        public MultiStarLayerPreset preset;

        /// <summary>
        /// Configuration for a single depth level within the multi-layer.
        /// </summary>
        [System.Serializable]
        public struct DepthConfig
        {
            [Tooltip("Parallax depth - lower values = farther/slower, higher = closer/faster")]
            [HighPrecision(5, 0.00001f)]
            public float parallaxDepth;

            [Tooltip("Star density for this depth level")]
            [Range(1, 100)]
            public float density;

            [Tooltip("Minimum star size at this depth")]
            [Min(0)]
            public float sizeMin;

            [Tooltip("Maximum star size at this depth")]
            [Min(0)]
            public float sizeMax;

            [Tooltip("Color tint for stars at this depth")]
            public Color colorTint;

            [Tooltip("Random seed for star positions at this depth")]
            public int seed;

            /// <summary>
            /// Creates a default depth configuration.
            /// </summary>
            public static DepthConfig Default(int index)
            {
                return new DepthConfig
                {
                    parallaxDepth = 0.01f + index * 0.015f,
                    density = 30f - index * 5f,
                    sizeMin = 0.01f + index * 0.01f,
                    sizeMax = 0.05f + index * 0.03f,
                    colorTint = Color.white,
                    seed = index * 1000
                };
            }
        }

        [Header("Multi-Depth Configuration")]
        [Tooltip("Configure each depth level. Supports up to 8 depths in a single draw call.")]
        public List<DepthConfig> depths = new List<DepthConfig>
        {
            DepthConfig.Default(0),
            DepthConfig.Default(1),
            DepthConfig.Default(2),
            DepthConfig.Default(3)
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

        // Shader property IDs (cached for performance)
        private static readonly int DepthCountID = Shader.PropertyToID("_DepthCount");
        private static readonly int DepthParamsID = Shader.PropertyToID("_DepthParams");
        private static readonly int DepthColorsID = Shader.PropertyToID("_DepthColors");
        private static readonly int DepthSeedsID = Shader.PropertyToID("_DepthSeeds");
        private static readonly int SpawnChanceID = Shader.PropertyToID("_SpawnChance");
        private static readonly int StarBrightnessMinID = Shader.PropertyToID("_StarBrightnessMin");
        private static readonly int StarBrightnessMaxID = Shader.PropertyToID("_StarBrightnessMax");
        private static readonly int BrightnessDistributionID = Shader.PropertyToID("_BrightnessDistribution");
        private static readonly int TwinkleSpeedID = Shader.PropertyToID("_TwinkleSpeed");
        private static readonly int TwinkleAmountID = Shader.PropertyToID("_TwinkleAmount");
        private static readonly int ColorVariationID = Shader.PropertyToID("_ColorVariation");
        private static readonly int WarmCoolMixID = Shader.PropertyToID("_WarmCoolMix");
        private static readonly int EdgeSharpnessID = Shader.PropertyToID("_EdgeSharpness");
        private static readonly int SizeDistributionID = Shader.PropertyToID("_SizeDistribution");
        private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
        private static readonly int RenderBackgroundID = Shader.PropertyToID("_RenderBackground");
        private static readonly int ClusterAmountID = Shader.PropertyToID("_ClusterAmount");
        private static readonly int ClusterScaleID = Shader.PropertyToID("_ClusterScale");

        // Pre-allocated arrays to avoid GC allocations
        private Vector4[] _depthParams = new Vector4[8];
        private Vector4[] _depthColors = new Vector4[8];
        private float[] _depthSeeds = new float[8];

        public override Shader GetShader()
        {
            return Shader.Find("Starfire/StarfieldMultiLayer");
        }

        public override void ConfigureMaterial(Material material)
        {
            // Apply preset if assigned
            if (preset != null)
            {
                preset.ApplyTo(this);
            }

            // Set depth count (clamped to max 8)
            int depthCount = Mathf.Min(depths.Count, 8);
            material.SetInt(DepthCountID, depthCount);

            // Populate depth arrays
            for (int i = 0; i < 8; i++)
            {
                if (i < depths.Count)
                {
                    var depth = depths[i];
                    _depthParams[i] = new Vector4(
                        depth.parallaxDepth,
                        depth.density,
                        depth.sizeMin,
                        depth.sizeMax
                    );
                    _depthColors[i] = depth.colorTint;
                    _depthSeeds[i] = depth.seed;
                }
                else
                {
                    // Zero out unused slots
                    _depthParams[i] = Vector4.zero;
                    _depthColors[i] = Vector4.zero;
                    _depthSeeds[i] = 0;
                }
            }

            material.SetVectorArray(DepthParamsID, _depthParams);
            material.SetVectorArray(DepthColorsID, _depthColors);
            material.SetFloatArray(DepthSeedsID, _depthSeeds);

            // Set shared parameters
            material.SetFloat(SpawnChanceID, spawnChance);
            material.SetFloat(StarBrightnessMinID, brightnessMin);
            material.SetFloat(StarBrightnessMaxID, brightnessMax);
            material.SetFloat(BrightnessDistributionID, brightnessDistribution);
            material.SetFloat(TwinkleSpeedID, twinkleSpeed);
            material.SetFloat(TwinkleAmountID, twinkleAmount);
            material.SetFloat(ColorVariationID, colorVariation);
            material.SetFloat(WarmCoolMixID, warmCoolMix);
            material.SetFloat(EdgeSharpnessID, edgeSharpness);
            material.SetFloat(SizeDistributionID, sizeDistribution);
            material.SetColor(BackgroundColorID, renderBackground ? backgroundColor : Color.clear);
            material.SetFloat(RenderBackgroundID, renderBackground ? 1f : 0f);
            material.SetFloat(ClusterAmountID, clusterAmount);
            material.SetFloat(ClusterScaleID, clusterScale);
        }

        /// <summary>
        /// Add a new depth configuration at runtime.
        /// </summary>
        public void AddDepth(DepthConfig config)
        {
            if (depths.Count < 8)
            {
                depths.Add(config);
            }
            else
            {
                Debug.LogWarning("MultiStarLayer: Cannot add more than 8 depths.");
            }
        }

        /// <summary>
        /// Create default depth configurations for the specified count.
        /// </summary>
        public void SetupDefaultDepths(int count)
        {
            depths.Clear();
            count = Mathf.Clamp(count, 1, 8);

            for (int i = 0; i < count; i++)
            {
                depths.Add(DepthConfig.Default(i));
            }
        }
    }
}
