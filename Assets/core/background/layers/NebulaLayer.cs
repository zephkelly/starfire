using UnityEngine;
using Starfire.Core.Background;
using Starfire.Core.Background.Presets;

namespace Starfire.Core.Background.Layers
{
    /// <summary>
    /// A layer that renders a procedural nebula using FBM noise with domain warping.
    /// Creates cloud-like, Hubble-style nebula effects with gradient-based coloring.
    /// </summary>
    [System.Serializable]
    public class NebulaLayer : StarfieldLayer
    {
        [Header("Preset")]
        [Tooltip("Optional preset to override all settings")]
        public NebulaLayerPreset preset;

        [Header("Noise Configuration")]
        [Tooltip("Scale of the base noise pattern (smaller = larger clouds)")]
        [Range(0.1f, 10f)]
        public float noiseScale = 1.0f;

        [Tooltip("Number of FBM octaves (more = finer detail, higher cost)")]
        [Range(1, 8)]
        public int octaves = 5;

        [Tooltip("How much each octave contributes (0.5 = each octave half as strong)")]
        [Range(0.3f, 0.7f)]
        public float persistence = 0.5f;

        [Tooltip("Frequency multiplier per octave (2 = each octave twice the frequency)")]
        [Range(1.5f, 3f)]
        public float lacunarity = 2.0f;

        [Header("Domain Warping")]
        [Tooltip("How much the noise distorts itself for organic shapes")]
        [Range(0f, 2f)]
        public float warpStrength = 0.5f;

        [Tooltip("Scale of the warping noise")]
        [Range(0.1f, 5f)]
        public float warpScale = 0.5f;

        [Header("Color Gradient")]
        [Tooltip("Number of colors in the gradient (2-4)")]
        [Range(2, 4)]
        public int colorCount = 3;

        [Tooltip("Outer/faint regions of the nebula")]
        public Color color1 = new Color(0.1f, 0.05f, 0.2f);

        [Tooltip("Mid-tone color")]
        public Color color2 = new Color(0.4f, 0.1f, 0.3f);

        [Tooltip("Brighter regions")]
        public Color color3 = new Color(0.8f, 0.3f, 0.4f);

        [Tooltip("Core/brightest regions")]
        public Color color4 = new Color(1f, 0.8f, 0.6f);

        [Tooltip("Shifts gradient towards outer (< 1) or inner (> 1) colors")]
        [Range(0.1f, 3f)]
        public float gradientBias = 1.0f;

        [Tooltip("Increases color separation in the gradient")]
        [Range(0.5f, 3f)]
        public float gradientContrast = 1.5f;

        [Header("Emission")]
        [Tooltip("Overall brightness/glow intensity")]
        [Range(0f, 3f)]
        public float emissionIntensity = 1.0f;

        [Tooltip("Extra brightness boost in the brightest regions")]
        [Range(1f, 5f)]
        public float coreEmissionBoost = 2.0f;

        [Header("Density and Shape")]
        [Tooltip("Overall nebula density/coverage")]
        [Range(0f, 2f)]
        public float density = 1.0f;

        [Tooltip("How soft the edges of visible nebula regions are")]
        [Range(0.1f, 2f)]
        public float edgeSoftness = 0.5f;

        [Tooltip("Minimum noise value to be visible (cuts off faint regions)")]
        [Range(0f, 0.5f)]
        public float threshold = 0.1f;

        [Tooltip("Frequency multiplier for fine detail layer")]
        [Range(0.5f, 4f)]
        public float detailFrequency = 2.0f;

        [Header("Background")]
        [Tooltip("Only enable for the first/bottom layer to fill background")]
        public bool renderBackground = false;

        public Color backgroundColor = Color.black;

        [Header("Seed")]
        [Tooltip("Random seed for unique nebula patterns")]
        public float seed = 0f;

        // Shader property IDs (cached for performance)
        private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
        private static readonly int OctavesID = Shader.PropertyToID("_Octaves");
        private static readonly int PersistenceID = Shader.PropertyToID("_Persistence");
        private static readonly int LacunarityID = Shader.PropertyToID("_Lacunarity");
        private static readonly int WarpStrengthID = Shader.PropertyToID("_WarpStrength");
        private static readonly int WarpScaleID = Shader.PropertyToID("_WarpScale");
        private static readonly int ColorCountID = Shader.PropertyToID("_ColorCount");
        private static readonly int Color1ID = Shader.PropertyToID("_Color1");
        private static readonly int Color2ID = Shader.PropertyToID("_Color2");
        private static readonly int Color3ID = Shader.PropertyToID("_Color3");
        private static readonly int Color4ID = Shader.PropertyToID("_Color4");
        private static readonly int GradientBiasID = Shader.PropertyToID("_GradientBias");
        private static readonly int GradientContrastID = Shader.PropertyToID("_GradientContrast");
        private static readonly int EmissionIntensityID = Shader.PropertyToID("_EmissionIntensity");
        private static readonly int CoreEmissionBoostID = Shader.PropertyToID("_CoreEmissionBoost");
        private static readonly int DensityID = Shader.PropertyToID("_Density");
        private static readonly int EdgeSoftnessID = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int ThresholdID = Shader.PropertyToID("_Threshold");
        private static readonly int DetailFrequencyID = Shader.PropertyToID("_DetailFrequency");
        private static readonly int ParallaxFactorID = Shader.PropertyToID("_ParallaxFactor");
        private static readonly int RenderBackgroundID = Shader.PropertyToID("_RenderBackground");
        private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
        private static readonly int SeedID = Shader.PropertyToID("_Seed");

        public override Shader GetShader()
        {
            return Shader.Find("Starfire/Nebula");
        }

        public override void ConfigureMaterial(Material material)
        {
            // Apply preset if assigned
            if (preset != null)
            {
                preset.ApplyTo(this);
            }

            material.SetFloat(NoiseScaleID, noiseScale);
            material.SetInt(OctavesID, octaves);
            material.SetFloat(PersistenceID, persistence);
            material.SetFloat(LacunarityID, lacunarity);
            material.SetFloat(WarpStrengthID, warpStrength);
            material.SetFloat(WarpScaleID, warpScale);
            material.SetInt(ColorCountID, colorCount);
            material.SetColor(Color1ID, color1);
            material.SetColor(Color2ID, color2);
            material.SetColor(Color3ID, color3);
            material.SetColor(Color4ID, color4);
            material.SetFloat(GradientBiasID, gradientBias);
            material.SetFloat(GradientContrastID, gradientContrast);
            material.SetFloat(EmissionIntensityID, emissionIntensity);
            material.SetFloat(CoreEmissionBoostID, coreEmissionBoost);
            material.SetFloat(DensityID, density);
            material.SetFloat(EdgeSoftnessID, edgeSoftness);
            material.SetFloat(ThresholdID, threshold);
            material.SetFloat(DetailFrequencyID, detailFrequency);
            material.SetFloat(ParallaxFactorID, parallaxDepth);
            material.SetFloat(RenderBackgroundID, renderBackground ? 1f : 0f);
            material.SetColor(BackgroundColorID, renderBackground ? backgroundColor : Color.clear);
            material.SetFloat(SeedID, seed);
        }
    }
}
