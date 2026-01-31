using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Background.Presets;

namespace Starfire.Core.Background.Layers
{
    /// <summary>
    /// Configuration for a single star shape within a ShapedStarLayer.
    /// </summary>
    [System.Serializable]
    public struct ShapeConfig
    {
        [Tooltip("The shape type to render")]
        public StarShape shape;

        [Tooltip("Relative spawn weight (higher = more likely to spawn)")]
        [Range(0f, 1f)]
        public float spawnWeight;

        [Tooltip("Use layer's shared size/color settings instead of per-shape overrides")]
        public bool useSharedSettings;

        [Header("Per-Shape Overrides (when useSharedSettings = false)")]
        [Min(0)]
        public float sizeMin;

        [Min(0)]
        public float sizeMax;

        [Range(0, 1)]
        public float edgeSharpness;

        public Color colorTint;

        /// <summary>
        /// Create a default configuration for a shape.
        /// </summary>
        public static ShapeConfig Default(StarShape shape, float weight)
        {
            return new ShapeConfig
            {
                shape = shape,
                spawnWeight = weight,
                useSharedSettings = true,
                sizeMin = 0.02f,
                sizeMax = 0.15f,
                edgeSharpness = 0f,
                colorTint = Color.white
            };
        }
    }

    /// <summary>
    /// Configuration for a single depth level within the shaped star layer.
    /// </summary>
    [System.Serializable]
    public struct ShapedDepthConfig
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
        public static ShapedDepthConfig Default(int index)
        {
            return new ShapedDepthConfig
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

    /// <summary>
    /// A layer that renders procedural stars with multiple shape types.
    /// Each shape can have its own spawn probability and optional visual overrides.
    /// Supports multiple depth levels rendered in a single draw call.
    /// </summary>
    [System.Serializable]
    public class ShapedStarLayer : StarfieldLayer
    {
        [Header("Preset")]
        [Tooltip("Optional preset to override all settings")]
        public ShapedStarLayerPreset preset;

        private const int MaxShapes = 4;
        private const int MaxDepths = 8;

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

        [Header("Multi-Depth Configuration")]
        [Tooltip("When populated, renders multiple depth layers in a single draw call. Leave empty for single-depth mode using the settings above.")]
        public List<ShapedDepthConfig> depths = new List<ShapedDepthConfig>();

        [Header("Distribution")]
        [Tooltip("Random seed for this layer's star positions (single-depth mode only)")]
        public int layerSeed = 0;

        [Tooltip("How much clustering affects star density (0 = uniform, 1 = heavily clustered)")]
        [Range(0f, 1f)]
        public float clusterAmount = 0.3f;

        [Tooltip("Scale of clusters (smaller = tighter clusters, larger = broader regions)")]
        [Min(0.01f)]
        public float clusterScale = 0.05f;

        // Tracks whether shape/depth config arrays need re-uploading to GPU
        [System.NonSerialized] private bool _configDirty = true;

        // Pre-allocated arrays for shader data
        private Vector4[] _shapeParams = new Vector4[MaxShapes];
        private Vector4[] _shapeVisuals = new Vector4[MaxShapes];
        private Vector4[] _shapeColors = new Vector4[MaxShapes];

        // Pre-allocated depth arrays
        private Vector4[] _depthParams = new Vector4[MaxDepths];
        private Vector4[] _depthColors = new Vector4[MaxDepths];
        private float[] _depthSeeds = new float[MaxDepths];
        private Vector4[] _depthParallaxOffsets = new Vector4[MaxDepths];

        // Shader property IDs (cached for performance)
        private static readonly int StarDensityID = Shader.PropertyToID("_StarDensity");
        private static readonly int SpawnChanceID = Shader.PropertyToID("_SpawnChance");
        private static readonly int StarBrightnessMinID = Shader.PropertyToID("_StarBrightnessMin");
        private static readonly int StarBrightnessMaxID = Shader.PropertyToID("_StarBrightnessMax");
        private static readonly int BrightnessDistributionID = Shader.PropertyToID("_BrightnessDistribution");
        private static readonly int StarSizeMinID = Shader.PropertyToID("_StarSizeMin");
        private static readonly int StarSizeMaxID = Shader.PropertyToID("_StarSizeMax");
        private static readonly int SizeDistributionID = Shader.PropertyToID("_SizeDistribution");
        private static readonly int TwinkleSpeedID = Shader.PropertyToID("_TwinkleSpeed");
        private static readonly int TwinkleAmountID = Shader.PropertyToID("_TwinkleAmount");
        private static readonly int StarColorID = Shader.PropertyToID("_StarColor");
        private static readonly int ColorVariationID = Shader.PropertyToID("_ColorVariation");
        private static readonly int WarmCoolMixID = Shader.PropertyToID("_WarmCoolMix");
        private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
        private static readonly int ParallaxFactorID = Shader.PropertyToID("_ParallaxFactor");
        private static readonly int RenderBackgroundID = Shader.PropertyToID("_RenderBackground");
        private static readonly int LayerSeedID = Shader.PropertyToID("_LayerSeed");
        private static readonly int ClusterAmountID = Shader.PropertyToID("_ClusterAmount");
        private static readonly int ClusterScaleID = Shader.PropertyToID("_ClusterScale");

        // Depth-specific property IDs
        private static readonly int DepthCountID = Shader.PropertyToID("_DepthCount");
        private static readonly int DepthParamsID = Shader.PropertyToID("_DepthParams");
        private static readonly int DepthColorsID = Shader.PropertyToID("_DepthColors");
        private static readonly int DepthSeedsID = Shader.PropertyToID("_DepthSeeds");
        private static readonly int DepthParallaxOffsetsID = Shader.PropertyToID("_DepthParallaxOffsets");

        // Shape-specific property IDs
        private static readonly int ShapeCountID = Shader.PropertyToID("_ShapeCount");
        private static readonly int ShapeParamsID = Shader.PropertyToID("_ShapeParams");
        private static readonly int ShapeVisualsID = Shader.PropertyToID("_ShapeVisuals");
        private static readonly int ShapeColorsID = Shader.PropertyToID("_ShapeColors");
        private static readonly int CumulativeWeightsID = Shader.PropertyToID("_CumulativeWeights");

        public override void MarkDirty()
        {
            base.MarkDirty();
            _configDirty = true;
        }

        public override Shader GetShader()
        {
            return Shader.Find("Starfire/ShapedStarfield");
        }

        public override void ConfigureMaterial(Material material)
        {
            // Apply preset if assigned (only when config changed)
            if (_configDirty && preset != null)
            {
                preset.ApplyTo(this);
            }

            // Config-dependent properties — only set when config actually changes
            if (_configDirty)
            {
                material.SetFloat(StarDensityID, density);
                material.SetFloat(SpawnChanceID, spawnChance);
                material.SetFloat(StarBrightnessMinID, brightnessMin);
                material.SetFloat(StarBrightnessMaxID, brightnessMax);
                material.SetFloat(BrightnessDistributionID, brightnessDistribution);
                material.SetFloat(StarSizeMinID, sizeMin);
                material.SetFloat(StarSizeMaxID, sizeMax);
                material.SetFloat(SizeDistributionID, sizeDistribution);
                material.SetFloat(TwinkleSpeedID, twinkleSpeed);
                material.SetFloat(TwinkleAmountID, twinkleAmount);
                material.SetColor(StarColorID, starColor);
                material.SetFloat(ColorVariationID, colorVariation);
                material.SetFloat(WarmCoolMixID, warmCoolMix);
                material.SetColor(BackgroundColorID, renderBackground ? backgroundColor : Color.clear);
                material.SetFloat(ParallaxFactorID, parallaxDepth);
                material.SetFloat(RenderBackgroundID, renderBackground ? 1f : 0f);
                material.SetFloat(LayerSeedID, layerSeed);
                material.SetFloat(ClusterAmountID, clusterAmount);
                material.SetFloat(ClusterScaleID, clusterScale);

                // Configure per-shape arrays (expensive SetVectorArray/SetFloatArray calls)
                ConfigureShapeData(material);

                _configDirty = false;
            }

            // Position-dependent properties — updated every dirty frame
            ApplyParallaxOffset(material);
            UpdateDepthParallaxOffsets(material);
            ApplyFabricProperties(material);
        }

        private void ConfigureShapeData(Material material)
        {
            int shapeCount = Mathf.Min(shapes.Count, MaxShapes);
            material.SetInt(ShapeCountID, shapeCount);

            // Calculate total weight for normalization
            float totalWeight = 0f;
            for (int i = 0; i < shapeCount; i++)
            {
                totalWeight += shapes[i].spawnWeight;
            }

            // Avoid division by zero
            if (totalWeight <= 0f)
            {
                totalWeight = 1f;
            }

            // Build cumulative weights and per-shape arrays
            float cumulative = 0f;
            Vector4 cumulativeWeights = Vector4.zero;

            for (int i = 0; i < MaxShapes; i++)
            {
                if (i < shapeCount)
                {
                    var shape = shapes[i];
                    cumulative += shape.spawnWeight / totalWeight;

                    // Determine which settings to use
                    float useSizeMin = shape.useSharedSettings ? sizeMin : shape.sizeMin;
                    float useSizeMax = shape.useSharedSettings ? sizeMax : shape.sizeMax;
                    float useEdgeSharpness = shape.useSharedSettings ? edgeSharpness : shape.edgeSharpness;
                    Color useColor = shape.useSharedSettings ? starColor : shape.colorTint;

                    // Pack shape params: x=type, y=weight, z=sizeMin, w=sizeMax
                    _shapeParams[i] = new Vector4(
                        (float)shape.shape,
                        shape.spawnWeight,
                        useSizeMin,
                        useSizeMax
                    );

                    // Pack shape visuals: x=edgeSharpness, y=colorVariation, z=brightnessMin, w=brightnessMax
                    _shapeVisuals[i] = new Vector4(
                        useEdgeSharpness,
                        colorVariation,
                        brightnessMin,
                        brightnessMax
                    );

                    // Pack shape color
                    _shapeColors[i] = useColor;

                    // Set cumulative weight
                    switch (i)
                    {
                        case 0: cumulativeWeights.x = cumulative; break;
                        case 1: cumulativeWeights.y = cumulative; break;
                        case 2: cumulativeWeights.z = cumulative; break;
                        case 3: cumulativeWeights.w = cumulative; break;
                    }
                }
                else
                {
                    // Zero out unused slots
                    _shapeParams[i] = Vector4.zero;
                    _shapeVisuals[i] = Vector4.zero;
                    _shapeColors[i] = Vector4.zero;
                }
            }

            // Set arrays to material
            material.SetVectorArray(ShapeParamsID, _shapeParams);
            material.SetVectorArray(ShapeVisualsID, _shapeVisuals);
            material.SetVectorArray(ShapeColorsID, _shapeColors);
            material.SetVector(CumulativeWeightsID, cumulativeWeights);

            // Configure depth config arrays (expensive SetVectorArray/SetFloatArray calls)
            ConfigureDepthData(material);
        }

        private void ConfigureDepthData(Material material)
        {
            int depthCount = Mathf.Min(depths.Count, MaxDepths);
            material.SetFloat(DepthCountID, depthCount);

            if (depthCount == 0) return;

            for (int i = 0; i < MaxDepths; i++)
            {
                if (i < depthCount)
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
                    _depthParams[i] = Vector4.zero;
                    _depthColors[i] = Vector4.zero;
                    _depthSeeds[i] = 0;
                }
            }

            material.SetVectorArray(DepthParamsID, _depthParams);
            material.SetVectorArray(DepthColorsID, _depthColors);
            material.SetFloatArray(DepthSeedsID, _depthSeeds);
        }

        /// <summary>
        /// Update only the per-depth parallax offsets (position-dependent, changes every frame).
        /// Separated from ConfigureDepthData to avoid redundant SetVectorArray calls for static config.
        /// </summary>
        private void UpdateDepthParallaxOffsets(Material material)
        {
            int depthCount = Mathf.Min(depths.Count, MaxDepths);
            if (depthCount == 0) return;

            for (int i = 0; i < MaxDepths; i++)
            {
                if (i < depthCount)
                {
                    Vector2 offset = ComputeParallaxOffset(depths[i].parallaxDepth);
                    _depthParallaxOffsets[i] = new Vector4(offset.x, offset.y, 0, 0);
                }
                else
                {
                    _depthParallaxOffsets[i] = Vector4.zero;
                }
            }

            material.SetVectorArray(DepthParallaxOffsetsID, _depthParallaxOffsets);
        }
    }
}
