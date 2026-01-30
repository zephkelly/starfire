using UnityEngine;
using Starfire.Core.Background.Presets;

namespace Starfire.Core.Background.Layers
{
    /// <summary>
    /// A layer that renders sparse, realistic procedural galaxies with spiral arms,
    /// dense star populations with varied colors/sizes, supergiants with diffraction spikes,
    /// gas dust lanes, and bright cores at very far parallax depth.
    /// </summary>
    [System.Serializable]
    public class GalaxyLayer : StarfieldLayer
    {
        [Header("Preset")]
        [Tooltip("Optional preset to override all settings")]
        public GalaxyLayerPreset preset;

        // === Galaxy Placement ===
        [Header("Galaxy Placement")]
        [Tooltip("Cell size for placement grid (larger = more sparse)")]
        [Range(1f, 20f)]
        public float galaxyCellSize = 8f;

        [Tooltip("Probability of a galaxy spawning in each cell")]
        [Range(0f, 1f)]
        public float galaxySpawnChance = 0.3f;

        [Tooltip("Minimum galaxy size")]
        [Range(0.05f, 2f)]
        public float galaxySizeMin = 0.3f;

        [Tooltip("Maximum galaxy size")]
        [Range(0.1f, 4f)]
        public float galaxySizeMax = 1.0f;

        // === Spiral Structure ===
        [Header("Spiral Structure")]
        [Tooltip("Number of spiral arms")]
        [Range(1f, 6f)]
        public float armCount = 2f;

        [Tooltip("How tightly the arms wind")]
        [Range(0.1f, 3f)]
        public float armWinding = 0.8f;

        [Tooltip("Width of spiral arms")]
        [Range(0.05f, 1f)]
        public float armSpread = 0.3f;

        [Tooltip("How quickly arm density falls off with radius")]
        [Range(0.5f, 5f)]
        public float armFalloff = 2f;

        // === Core ===
        [Header("Core")]
        [Tooltip("Size of the central bulge")]
        [Range(0.01f, 0.5f)]
        public float coreSize = 0.12f;

        [Tooltip("Brightness of the central bulge")]
        [Range(0.5f, 5f)]
        public float coreBrightness = 2.5f;

        [Tooltip("Falloff sharpness of core")]
        [Range(1f, 8f)]
        public float coreFalloff = 3f;

        // === Star Population ===
        [Header("Star Population")]
        [Tooltip("Density of stars (grid resolution)")]
        [Range(20f, 400f)]
        public float starDensity = 120f;

        [Tooltip("Overall star brightness")]
        [Range(0.1f, 5f)]
        public float starBrightness = 1.5f;

        [Tooltip("Minimum star size")]
        [Range(0.01f, 0.2f)]
        public float starSizeMin = 0.05f;

        [Tooltip("Maximum star size")]
        [Range(0.05f, 0.8f)]
        public float starSizeMax = 0.3f;

        [Tooltip("How stars thin out from center")]
        [Range(0.5f, 5f)]
        public float starFalloff = 1.5f;

        [Tooltip("How strongly stars cluster along spiral arms")]
        [Range(1f, 15f)]
        public float starConcentration = 5f;

        // === Bright Stars and Supergiants ===
        [Header("Bright Stars and Supergiants")]
        [Tooltip("Chance of a supergiant star in each sparse cell")]
        [Range(0f, 0.15f)]
        public float supergiantChance = 0.03f;

        [Tooltip("Size of supergiant stars")]
        [Range(0.1f, 1.5f)]
        public float supergiantSize = 0.6f;

        [Tooltip("Brightness of supergiant stars")]
        [Range(1f, 8f)]
        public float supergiantBrightness = 4f;

        [Tooltip("Length of diffraction spikes on supergiants")]
        [Range(0f, 0.5f)]
        public float spikeLength = 0.15f;

        [Tooltip("Width of diffraction spikes")]
        [Range(0.001f, 0.05f)]
        public float spikeWidth = 0.008f;

        // === Star Color Variation ===
        [Header("Star Color Variation")]
        [Tooltip("Fraction of stars that are blue")]
        [Range(0f, 1f)]
        public float blueStarChance = 0.3f;

        [Tooltip("Fraction of stars that are red")]
        [Range(0f, 1f)]
        public float redStarChance = 0.15f;

        [Tooltip("Fraction of stars that are yellow")]
        [Range(0f, 1f)]
        public float yellowStarChance = 0.25f;

        [Tooltip("Blue star color")]
        public Color colorBlue = new Color(0.6f, 0.7f, 1f);

        [Tooltip("Red giant color")]
        public Color colorRed = new Color(1f, 0.5f, 0.3f);

        [Tooltip("Yellow star color")]
        public Color colorYellow = new Color(1f, 0.95f, 0.7f);

        [Tooltip("White star color")]
        public Color colorWhite = Color.white;

        // === Dust and Gas ===
        [Header("Dust and Gas")]
        [Tooltip("Intensity of gas/dust glow along arms")]
        [Range(0f, 2f)]
        public float dustIntensity = 0.5f;

        [Tooltip("Scale of dust noise pattern")]
        [Range(1f, 10f)]
        public float dustNoiseScale = 4f;

        [Tooltip("Domain warp strength for dust")]
        [Range(0f, 1f)]
        public float dustWarpStrength = 0.3f;

        // === Halo ===
        [Header("Halo")]
        [Tooltip("Extent of the outer halo")]
        [Range(0.1f, 2f)]
        public float haloSize = 0.8f;

        [Tooltip("Brightness of outer halo")]
        [Range(0f, 1f)]
        public float haloIntensity = 0.15f;

        [Tooltip("Falloff sharpness of halo")]
        [Range(1f, 6f)]
        public float haloFalloff = 2.5f;

        // === Color ===
        [Header("Galaxy Color")]
        [Tooltip("Color of the galaxy core")]
        public Color colorCore = new Color(1f, 0.95f, 0.8f);

        [Tooltip("Color of the spiral arms")]
        public Color colorMid = new Color(0.6f, 0.7f, 1f);

        [Tooltip("Color of the outer halo")]
        public Color colorOuter = new Color(0.3f, 0.35f, 0.6f);

        [Tooltip("Overall brightness multiplier")]
        [Range(0.1f, 3f)]
        public float overallBrightness = 1f;

        [Tooltip("Overall opacity (lower = more distant appearance)")]
        [Range(0f, 1f)]
        public float opacity = 1f;

        // === Variation ===
        [Header("Variation")]
        [Tooltip("How much galaxies can be elliptical (0 = all circular, 0.6 = very elliptical)")]
        [Range(0f, 0.6f)]
        public float ellipticityRange = 0.3f;

        [Tooltip("How much galaxies vary in tilt angle")]
        [Range(0f, 1f)]
        public float tiltRange = 1f;

        // === Zoom ===
        [Header("Zoom")]
        [Tooltip("How much the galaxy layer responds to camera zoom (0 = no zoom, 1 = full zoom). Overrides parallax-based zoom which is too low for distant layers.")]
        [Range(0f, 1f)]
        public float zoomResponse = 0.5f;

        // === Background ===
        [Header("Background")]
        [Tooltip("Render solid background")]
        public bool renderBackground = false;
        public Color backgroundColor = Color.black;

        // === Seed ===
        [Header("Seed")]
        [Tooltip("Random seed for unique galaxy patterns")]
        public float seed = 0f;

        // Shader property IDs
        private static readonly int GalaxyCellSizeID = Shader.PropertyToID("_GalaxyCellSize");
        private static readonly int GalaxySpawnChanceID = Shader.PropertyToID("_GalaxySpawnChance");
        private static readonly int GalaxySizeMinID = Shader.PropertyToID("_GalaxySizeMin");
        private static readonly int GalaxySizeMaxID = Shader.PropertyToID("_GalaxySizeMax");
        private static readonly int ArmCountID = Shader.PropertyToID("_ArmCount");
        private static readonly int ArmWindingID = Shader.PropertyToID("_ArmWinding");
        private static readonly int ArmSpreadID = Shader.PropertyToID("_ArmSpread");
        private static readonly int ArmFalloffID = Shader.PropertyToID("_ArmFalloff");
        private static readonly int CoreSizeID = Shader.PropertyToID("_CoreSize");
        private static readonly int CoreBrightnessID = Shader.PropertyToID("_CoreBrightness");
        private static readonly int CoreFalloffID = Shader.PropertyToID("_CoreFalloff");
        private static readonly int StarDensityID = Shader.PropertyToID("_StarDensity");
        private static readonly int StarBrightnessID = Shader.PropertyToID("_StarBrightness");
        private static readonly int StarSizeMinID = Shader.PropertyToID("_StarSizeMin");
        private static readonly int StarSizeMaxID = Shader.PropertyToID("_StarSizeMax");
        private static readonly int StarFalloffID = Shader.PropertyToID("_StarFalloff");
        private static readonly int StarConcentrationID = Shader.PropertyToID("_StarConcentration");
        private static readonly int SupergiantChanceID = Shader.PropertyToID("_SupergiantChance");
        private static readonly int SupergiantSizeID = Shader.PropertyToID("_SupergiantSize");
        private static readonly int SupergiantBrightnessID = Shader.PropertyToID("_SupergiantBrightness");
        private static readonly int SpikeLengthID = Shader.PropertyToID("_SpikeLength");
        private static readonly int SpikeWidthID = Shader.PropertyToID("_SpikeWidth");
        private static readonly int BlueStarChanceID = Shader.PropertyToID("_BlueStarChance");
        private static readonly int RedStarChanceID = Shader.PropertyToID("_RedStarChance");
        private static readonly int YellowStarChanceID = Shader.PropertyToID("_YellowStarChance");
        private static readonly int ColorBlueID = Shader.PropertyToID("_ColorBlue");
        private static readonly int ColorRedID = Shader.PropertyToID("_ColorRed");
        private static readonly int ColorYellowID = Shader.PropertyToID("_ColorYellow");
        private static readonly int ColorWhiteID = Shader.PropertyToID("_ColorWhite");
        private static readonly int DustIntensityID = Shader.PropertyToID("_DustIntensity");
        private static readonly int DustNoiseScaleID = Shader.PropertyToID("_DustNoiseScale");
        private static readonly int DustWarpStrengthID = Shader.PropertyToID("_DustWarpStrength");
        private static readonly int HaloSizeID = Shader.PropertyToID("_HaloSize");
        private static readonly int HaloIntensityID = Shader.PropertyToID("_HaloIntensity");
        private static readonly int HaloFalloffID = Shader.PropertyToID("_HaloFalloff");
        private static readonly int ColorCoreID = Shader.PropertyToID("_ColorCore");
        private static readonly int ColorMidID = Shader.PropertyToID("_ColorMid");
        private static readonly int ColorOuterID = Shader.PropertyToID("_ColorOuter");
        private static readonly int OverallBrightnessID = Shader.PropertyToID("_OverallBrightness");
        private static readonly int OpacityID = Shader.PropertyToID("_Opacity");
        private static readonly int EllipticityRangeID = Shader.PropertyToID("_EllipticityRange");
        private static readonly int TiltRangeID = Shader.PropertyToID("_TiltRange");
        private static readonly int ParallaxFactorID = Shader.PropertyToID("_ParallaxFactor");
        private static readonly int ZoomResponseID = Shader.PropertyToID("_ZoomResponse");
        private static readonly int RenderBackgroundID = Shader.PropertyToID("_RenderBackground");
        private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
        private static readonly int SeedID = Shader.PropertyToID("_Seed");

        public override Shader GetShader()
        {
            return Shader.Find("Starfire/Galaxy");
        }

        public override void ConfigureMaterial(Material material)
        {
            if (preset != null)
            {
                preset.ApplyTo(this);
            }

            material.SetFloat(GalaxyCellSizeID, galaxyCellSize);
            material.SetFloat(GalaxySpawnChanceID, galaxySpawnChance);
            material.SetFloat(GalaxySizeMinID, galaxySizeMin);
            material.SetFloat(GalaxySizeMaxID, galaxySizeMax);
            material.SetFloat(ArmCountID, armCount);
            material.SetFloat(ArmWindingID, armWinding);
            material.SetFloat(ArmSpreadID, armSpread);
            material.SetFloat(ArmFalloffID, armFalloff);
            material.SetFloat(CoreSizeID, coreSize);
            material.SetFloat(CoreBrightnessID, coreBrightness);
            material.SetFloat(CoreFalloffID, coreFalloff);
            material.SetFloat(StarDensityID, starDensity);
            material.SetFloat(StarBrightnessID, starBrightness);
            material.SetFloat(StarSizeMinID, starSizeMin);
            material.SetFloat(StarSizeMaxID, starSizeMax);
            material.SetFloat(StarFalloffID, starFalloff);
            material.SetFloat(StarConcentrationID, starConcentration);
            material.SetFloat(SupergiantChanceID, supergiantChance);
            material.SetFloat(SupergiantSizeID, supergiantSize);
            material.SetFloat(SupergiantBrightnessID, supergiantBrightness);
            material.SetFloat(SpikeLengthID, spikeLength);
            material.SetFloat(SpikeWidthID, spikeWidth);
            material.SetFloat(BlueStarChanceID, blueStarChance);
            material.SetFloat(RedStarChanceID, redStarChance);
            material.SetFloat(YellowStarChanceID, yellowStarChance);
            material.SetColor(ColorBlueID, colorBlue);
            material.SetColor(ColorRedID, colorRed);
            material.SetColor(ColorYellowID, colorYellow);
            material.SetColor(ColorWhiteID, colorWhite);
            material.SetFloat(DustIntensityID, dustIntensity);
            material.SetFloat(DustNoiseScaleID, dustNoiseScale);
            material.SetFloat(DustWarpStrengthID, dustWarpStrength);
            material.SetFloat(HaloSizeID, haloSize);
            material.SetFloat(HaloIntensityID, haloIntensity);
            material.SetFloat(HaloFalloffID, haloFalloff);
            material.SetColor(ColorCoreID, colorCore);
            material.SetColor(ColorMidID, colorMid);
            material.SetColor(ColorOuterID, colorOuter);
            material.SetFloat(OverallBrightnessID, overallBrightness);
            material.SetFloat(OpacityID, opacity);
            material.SetFloat(EllipticityRangeID, ellipticityRange);
            material.SetFloat(TiltRangeID, tiltRange);
            material.SetFloat(ParallaxFactorID, parallaxDepth);
            material.SetFloat(ZoomResponseID, zoomResponse);
            material.SetFloat(RenderBackgroundID, renderBackground ? 1f : 0f);
            material.SetColor(BackgroundColorID, renderBackground ? backgroundColor : Color.clear);
            material.SetFloat(SeedID, seed);

            // Per-layer parallax offset (double-precision, fmod'd for float safety)
            ApplyParallaxOffset(material);

            ApplyFabricProperties(material);
        }
    }
}
