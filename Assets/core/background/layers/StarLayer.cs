using UnityEngine;

namespace Starfire.Core.Background.Layers
{
    /// <summary>
    /// A layer that renders procedural stars using the Starfield shader.
    /// </summary>
    [System.Serializable]
    public class StarLayer : StarfieldLayer
    {
        [Header("Star Field")]
        [Range(1, 100)]
        public float density = 20f;

        [Range(0, 1)]
        public float spawnChance = 0.8f;

        [Range(0.1f, 2f)]
        public float brightness = 1f;

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

        // Shader property IDs (cached for performance)
        private static readonly int StarDensityID = Shader.PropertyToID("_StarDensity");
        private static readonly int SpawnChanceID = Shader.PropertyToID("_SpawnChance");
        private static readonly int StarBrightnessID = Shader.PropertyToID("_StarBrightness");
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

        public override Shader GetShader()
        {
            return Shader.Find("Starfire/Starfield");
        }

        public override void ConfigureMaterial(Material material)
        {
            material.SetFloat(StarDensityID, density);
            material.SetFloat(SpawnChanceID, spawnChance);
            material.SetFloat(StarBrightnessID, brightness);
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
        }
    }
}
