using UnityEngine;
using Starfire.Core.Background.Presets;

namespace Starfire.Core.Background.Layers
{
    /// <summary>
    /// A layer that renders procedural stars using the Starfield shader.
    /// </summary>
    [System.Serializable]
    public class StarLayer : StarfieldLayer
    {
        [Header("Preset")]
        [Tooltip("Optional preset to override all settings")]
        public StarLayerPreset preset;

        [Header("Star Field")]
        [Range(1, 100)]
        public float density = 20f;

        [Range(0, 1)]
        public float spawnChance = 0.8f;

        [Header("Brightness")]
        [Range(0.1f, 2f)]
        public float brightnessMin = 0.3f;

        [Range(0.1f, 2f)]
        public float brightnessMax = 1f;

        [Tooltip("Distribution curve (0=favor dim, 0.5=uniform, 1=favor bright)")]
        [Range(0, 1)]
        public float brightnessDistribution = 0.5f;

        [Header("Star Size")]
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

        [Header("Color")]
        public Color starColor = Color.white;

        [Range(0, 1)]
        public float colorVariation = 0.3f;

        [Range(0, 1)]
        public float warmCoolMix = 0.5f;

        [Header("Shape")]
        [Range(0, 1)]
        public float edgeSharpness = 0f;

        [Header("Background")]
        [Tooltip("Only the first layer should render background color; others should be transparent")]
        public bool renderBackground = false;
        public Color backgroundColor = new Color(0, 0, 0.02f, 1);

        [Header("Distribution")]
        [Tooltip("Random seed for this layer's star positions (use different values per layer)")]
        public int layerSeed = 0;

        [Tooltip("How much clustering affects star density (0 = uniform, 1 = heavily clustered)")]
        [Range(0f, 1f)]
        public float clusterAmount = 0.3f;

        [Tooltip("Scale of clusters (smaller = tighter clusters, larger = broader regions)")]
        [Min(0.01f)]
        public float clusterScale = 0.05f;

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
        private static readonly int EdgeSharpnessID = Shader.PropertyToID("_EdgeSharpness");
        private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
        private static readonly int ParallaxFactorID = Shader.PropertyToID("_ParallaxFactor");
        private static readonly int RenderBackgroundID = Shader.PropertyToID("_RenderBackground");
        private static readonly int LayerSeedID = Shader.PropertyToID("_LayerSeed");
        private static readonly int ClusterAmountID = Shader.PropertyToID("_ClusterAmount");
        private static readonly int ClusterScaleID = Shader.PropertyToID("_ClusterScale");

        public override Shader GetShader()
        {
            return Shader.Find("Starfire/Starfield");
        }

        public override void ConfigureMaterial(Material material)
        {
            // Apply preset if assigned
            if (preset != null)
            {
                preset.ApplyTo(this);
            }

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
            material.SetFloat(EdgeSharpnessID, edgeSharpness);
            material.SetColor(BackgroundColorID, renderBackground ? backgroundColor : Color.clear);
            material.SetFloat(ParallaxFactorID, parallaxDepth);
            material.SetFloat(RenderBackgroundID, renderBackground ? 1f : 0f);
            material.SetFloat(LayerSeedID, layerSeed);
            material.SetFloat(ClusterAmountID, clusterAmount);
            material.SetFloat(ClusterScaleID, clusterScale);

            // Per-layer fabric sampling
            ApplyFabricProperties(material);
        }
    }
}
