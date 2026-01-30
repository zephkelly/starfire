using UnityEngine;
using Starfire.Core.Background;
using Starfire.Core.Background.Presets;

namespace Starfire.Core.Background.Layers
{
    public enum EdgeMode { Soft = 0, Sharp = 1, Mixed = 2 }

    /// <summary>
    /// A nebula layer with dramatic artistic effects including cosmic pillars,
    /// painterly posterization, wispy tendrils, and cellular/organic structures.
    /// </summary>
    [System.Serializable]
    public class NebulaLayer : StarfieldLayer
    {
        [Header("Preset")]
        [Tooltip("Optional preset to override all settings")]
        public NebulaLayerPreset preset;

        // === Style Features ===
        [Header("Style Features")]
        [Tooltip("Enable cosmic pillar structures with vertical silhouettes")]
        public bool enablePillars = false;

        [Tooltip("Enable painterly posterization effect")]
        public bool enablePainterly = false;

        [Tooltip("Enable wispy tendril patterns using curl noise")]
        public bool enableTendrils = false;

        [Tooltip("Enable cellular/organic bubble structures")]
        public bool enableCellular = false;

        // === Pillar Settings ===
        [Header("Pillar Settings")]
        [Tooltip("Vertical stretch factor for pillar structures")]
        [Range(1f, 5f)]
        public float pillarStretch = 2.5f;

        [Tooltip("Rotation angle of pillar direction")]
        [Range(-180f, 180f)]
        public float pillarAngle = 0f;

        [Tooltip("Asymmetric warp bias for pillar effect")]
        [Range(0f, 2f)]
        public float pillarWarpBias = 0.5f;

        // === Painterly Settings ===
        [Header("Painterly Settings")]
        [Tooltip("Number of posterization levels")]
        [Range(3, 12)]
        public int posterizeLevels = 6;

        [Tooltip("Scale of brush stroke patterns")]
        [Range(0.5f, 5f)]
        public float brushStrokeScale = 2f;

        [Tooltip("Amount of brush stroke warping")]
        [Range(0f, 1f)]
        public float brushWarpAmount = 0.3f;

        // === Tendril Settings ===
        [Header("Tendril Settings")]
        [Tooltip("Strength of curl-based tendril effect")]
        [Range(0f, 2f)]
        public float curlStrength = 0.5f;

        [Tooltip("Scale of curl noise")]
        [Range(0.5f, 4f)]
        public float curlScale = 1.5f;

        [Tooltip("Length of tendril trails")]
        [Range(0.1f, 2f)]
        public float tendrilLength = 0.8f;

        // === Cellular Settings ===
        [Header("Cellular Settings")]
        [Tooltip("Scale of Voronoi cell pattern")]
        [Range(2f, 20f)]
        public float voronoiScale = 8f;

        [Tooltip("Width of cell edge highlights")]
        [Range(0.01f, 0.5f)]
        public float cellEdgeWidth = 0.15f;

        [Tooltip("Invert cells for bubble/membrane effect")]
        public bool bubbleInvert = false;

        // === Internal Structure ===
        [Header("Internal Structure - Dense Cores")]
        [Tooltip("Enable bright dense cores with halos")]
        public bool enableDenseCores = false;

        [Tooltip("Intensity of core brightness boost")]
        [Range(0f, 2f)]
        public float coreIntensity = 1f;

        [Tooltip("Size of halo around cores")]
        [Range(0f, 0.5f)]
        public float haloSize = 0.2f;

        [Header("Internal Structure - Edge Lighting")]
        [Tooltip("Enable rim/edge lighting effect")]
        public bool enableEdgeLit = false;

        [Tooltip("Strength of rim lighting")]
        [Range(0f, 2f)]
        public float rimLightStrength = 0.5f;

        [Tooltip("Color of rim lighting")]
        public Color rimLightColor = new Color(1f, 0.9f, 0.7f);

        [Header("Internal Structure - Depth Bands")]
        [Tooltip("Enable layered depth band effect")]
        public bool enableDepthBands = false;

        [Tooltip("Number of depth bands")]
        [Range(2, 8)]
        public int bandCount = 4;

        [Tooltip("Contrast between bands")]
        [Range(0f, 1f)]
        public float bandContrast = 0.5f;

        [Header("Internal Structure - Bright Spots")]
        [Tooltip("Enable scattered bright spots (star-forming regions)")]
        public bool enableBrightSpots = false;

        [Tooltip("Density of bright spots")]
        [Range(5f, 50f)]
        public float spotDensity = 20f;

        [Tooltip("Intensity of bright spots")]
        [Range(0f, 3f)]
        public float spotIntensity = 1.5f;

        // === Edge Style ===
        [Header("Edge Style")]
        [Tooltip("Edge rendering mode")]
        public EdgeMode edgeMode = EdgeMode.Mixed;

        [Tooltip("Sharpness of outer silhouette edges")]
        [Range(0f, 1f)]
        public float silhouetteSharpness = 0.7f;

        [Tooltip("Softness of internal gradients")]
        [Range(0.1f, 1f)]
        public float internalSoftness = 0.5f;

        // === Base Noise ===
        [Header("Base Noise")]
        [Tooltip("Scale of the base noise pattern")]
        [Range(0.1f, 10f)]
        public float noiseScale = 1f;

        [Tooltip("Number of FBM octaves")]
        [Range(1, 6)]
        public int octaves = 4;

        [Tooltip("Persistence per octave")]
        [Range(0.3f, 0.7f)]
        public float persistence = 0.5f;

        [Tooltip("Frequency multiplier per octave")]
        [Range(1.5f, 3f)]
        public float lacunarity = 2f;

        [Tooltip("Domain warp strength")]
        [Range(0f, 2f)]
        public float warpStrength = 0.5f;

        [Tooltip("Domain warp scale")]
        [Range(0.1f, 5f)]
        public float warpScale = 0.5f;

        // === Color Gradient ===
        [Header("Color Gradient")]
        [Tooltip("Number of colors in gradient")]
        [Range(2, 4)]
        public int colorCount = 3;

        [Tooltip("Outer/faint color")]
        public Color color1 = new Color(0.1f, 0.05f, 0.2f);

        [Tooltip("Mid-tone color")]
        public Color color2 = new Color(0.4f, 0.1f, 0.3f);

        [Tooltip("Brighter color")]
        public Color color3 = new Color(0.8f, 0.3f, 0.4f);

        [Tooltip("Core/brightest color")]
        public Color color4 = new Color(1f, 0.8f, 0.6f);

        [Tooltip("Gradient bias")]
        [Range(0.1f, 3f)]
        public float gradientBias = 1f;

        [Tooltip("Gradient contrast")]
        [Range(0.5f, 3f)]
        public float gradientContrast = 1.5f;

        // === Emission ===
        [Header("Emission")]
        [Tooltip("Base emission intensity")]
        [Range(0f, 3f)]
        public float emissionIntensity = 1f;

        [Tooltip("Extra emission in bright regions")]
        [Range(1f, 5f)]
        public float coreEmissionBoost = 2f;

        // === Density ===
        [Header("Density")]
        [Tooltip("Overall nebula density")]
        [Range(0f, 2f)]
        public float density = 1f;

        [Tooltip("Visibility threshold")]
        [Range(0f, 0.5f)]
        public float threshold = 0.1f;

        // === Background ===
        [Header("Background")]
        [Tooltip("Render solid background")]
        public bool renderBackground = false;

        public Color backgroundColor = Color.black;

        // === Seed ===
        [Header("Seed")]
        [Tooltip("Random seed for unique patterns")]
        public float seed = 0f;

        // === Region Masking ===
        [Header("Region Masking")]
        [Tooltip("World-space center of the nebula region")]
        public Vector2 regionCenter = Vector2.zero;

        [Tooltip("Radius of the visible nebula region in world units")]
        [Min(1f)]
        public float regionRadius = 10000f;

        [Tooltip("Distance over which the nebula fades at the boundary")]
        [Min(0f)]
        public float regionFalloff = 50f;

        [Tooltip("How the region edges are rendered (0=Smooth, 1=Sharp, 2=Inverse)")]
        [Range(0, 2)]
        public int regionEdgeMode = 0;

        // === Fabric Color Distribution ===
        [Header("Fabric Color Distribution")]
        [Tooltip("How much to blend toward fabric-generated colors (0 = use authored colors, 1 = use fabric colors)")]
        [Range(0f, 1f)]
        public float fabricColorBlend = 0.8f;

        // Shader property IDs
        private static readonly int EnablePillarsID = Shader.PropertyToID("_EnablePillars");
        private static readonly int EnablePainterlyID = Shader.PropertyToID("_EnablePainterly");
        private static readonly int EnableTendrilsID = Shader.PropertyToID("_EnableTendrils");
        private static readonly int EnableCellularID = Shader.PropertyToID("_EnableCellular");
        private static readonly int PillarStretchID = Shader.PropertyToID("_PillarStretch");
        private static readonly int PillarAngleID = Shader.PropertyToID("_PillarAngle");
        private static readonly int PillarWarpBiasID = Shader.PropertyToID("_PillarWarpBias");
        private static readonly int PosterizeLevelsID = Shader.PropertyToID("_PosterizeLevels");
        private static readonly int BrushStrokeScaleID = Shader.PropertyToID("_BrushStrokeScale");
        private static readonly int BrushWarpAmountID = Shader.PropertyToID("_BrushWarpAmount");
        private static readonly int CurlStrengthID = Shader.PropertyToID("_CurlStrength");
        private static readonly int CurlScaleID = Shader.PropertyToID("_CurlScale");
        private static readonly int TendrilLengthID = Shader.PropertyToID("_TendrilLength");
        private static readonly int VoronoiScaleID = Shader.PropertyToID("_VoronoiScale");
        private static readonly int CellEdgeWidthID = Shader.PropertyToID("_CellEdgeWidth");
        private static readonly int BubbleInvertID = Shader.PropertyToID("_BubbleInvert");
        private static readonly int EnableDenseCoresID = Shader.PropertyToID("_EnableDenseCores");
        private static readonly int CoreIntensityID = Shader.PropertyToID("_CoreIntensity");
        private static readonly int HaloSizeID = Shader.PropertyToID("_HaloSize");
        private static readonly int EnableEdgeLitID = Shader.PropertyToID("_EnableEdgeLit");
        private static readonly int RimLightStrengthID = Shader.PropertyToID("_RimLightStrength");
        private static readonly int RimLightColorID = Shader.PropertyToID("_RimLightColor");
        private static readonly int EnableDepthBandsID = Shader.PropertyToID("_EnableDepthBands");
        private static readonly int BandCountID = Shader.PropertyToID("_BandCount");
        private static readonly int BandContrastID = Shader.PropertyToID("_BandContrast");
        private static readonly int EnableBrightSpotsID = Shader.PropertyToID("_EnableBrightSpots");
        private static readonly int SpotDensityID = Shader.PropertyToID("_SpotDensity");
        private static readonly int SpotIntensityID = Shader.PropertyToID("_SpotIntensity");
        private static readonly int EdgeModeID = Shader.PropertyToID("_EdgeMode");
        private static readonly int SilhouetteSharpnessID = Shader.PropertyToID("_SilhouetteSharpness");
        private static readonly int InternalSoftnessID = Shader.PropertyToID("_InternalSoftness");
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
        private static readonly int ThresholdID = Shader.PropertyToID("_Threshold");
        private static readonly int ParallaxFactorID = Shader.PropertyToID("_ParallaxFactor");
        private static readonly int RenderBackgroundID = Shader.PropertyToID("_RenderBackground");
        private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
        private static readonly int SeedID = Shader.PropertyToID("_Seed");
        private static readonly int RegionCenterID = Shader.PropertyToID("_RegionCenter");
        private static readonly int RegionRadiusID = Shader.PropertyToID("_RegionRadius");
        private static readonly int RegionFalloffID = Shader.PropertyToID("_RegionFalloff");
        private static readonly int RegionEdgeModeID = Shader.PropertyToID("_RegionEdgeMode");

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

            // Style Features
            material.SetFloat(EnablePillarsID, enablePillars ? 1f : 0f);
            material.SetFloat(EnablePainterlyID, enablePainterly ? 1f : 0f);
            material.SetFloat(EnableTendrilsID, enableTendrils ? 1f : 0f);
            material.SetFloat(EnableCellularID, enableCellular ? 1f : 0f);

            // Pillar Settings
            material.SetFloat(PillarStretchID, pillarStretch);
            material.SetFloat(PillarAngleID, pillarAngle);
            material.SetFloat(PillarWarpBiasID, pillarWarpBias);

            // Painterly Settings
            material.SetInt(PosterizeLevelsID, posterizeLevels);
            material.SetFloat(BrushStrokeScaleID, brushStrokeScale);
            material.SetFloat(BrushWarpAmountID, brushWarpAmount);

            // Tendril Settings
            material.SetFloat(CurlStrengthID, curlStrength);
            material.SetFloat(CurlScaleID, curlScale);
            material.SetFloat(TendrilLengthID, tendrilLength);

            // Cellular Settings
            material.SetFloat(VoronoiScaleID, voronoiScale);
            material.SetFloat(CellEdgeWidthID, cellEdgeWidth);
            material.SetFloat(BubbleInvertID, bubbleInvert ? 1f : 0f);

            // Internal Structure
            material.SetFloat(EnableDenseCoresID, enableDenseCores ? 1f : 0f);
            material.SetFloat(CoreIntensityID, coreIntensity);
            material.SetFloat(HaloSizeID, haloSize);
            material.SetFloat(EnableEdgeLitID, enableEdgeLit ? 1f : 0f);
            material.SetFloat(RimLightStrengthID, rimLightStrength);
            material.SetColor(RimLightColorID, rimLightColor);
            material.SetFloat(EnableDepthBandsID, enableDepthBands ? 1f : 0f);
            material.SetInt(BandCountID, bandCount);
            material.SetFloat(BandContrastID, bandContrast);
            material.SetFloat(EnableBrightSpotsID, enableBrightSpots ? 1f : 0f);
            material.SetFloat(SpotDensityID, spotDensity);
            material.SetFloat(SpotIntensityID, spotIntensity);

            // Edge Style
            material.SetInt(EdgeModeID, (int)edgeMode);
            material.SetFloat(SilhouetteSharpnessID, silhouetteSharpness);
            material.SetFloat(InternalSoftnessID, internalSoftness);

            // Base Noise
            material.SetFloat(NoiseScaleID, noiseScale);
            material.SetInt(OctavesID, octaves);
            material.SetFloat(PersistenceID, persistence);
            material.SetFloat(LacunarityID, lacunarity);
            material.SetFloat(WarpStrengthID, warpStrength);
            material.SetFloat(WarpScaleID, warpScale);

            // Color Gradient
            material.SetInt(ColorCountID, colorCount);
            material.SetColor(Color1ID, color1);
            material.SetColor(Color2ID, color2);
            material.SetColor(Color3ID, color3);
            material.SetColor(Color4ID, color4);
            material.SetFloat(GradientBiasID, gradientBias);
            material.SetFloat(GradientContrastID, gradientContrast);

            // Emission
            material.SetFloat(EmissionIntensityID, emissionIntensity);
            material.SetFloat(CoreEmissionBoostID, coreEmissionBoost);

            // Density
            material.SetFloat(DensityID, density);
            material.SetFloat(ThresholdID, threshold);

            // Parallax & Background
            material.SetFloat(ParallaxFactorID, parallaxDepth);
            material.SetFloat(RenderBackgroundID, renderBackground ? 1f : 0f);
            material.SetColor(BackgroundColorID, renderBackground ? backgroundColor : Color.clear);

            // Per-layer parallax offset (double-precision, fmod'd for float safety)
            ApplyParallaxOffset(material);

            // Seed
            material.SetFloat(SeedID, seed);

            // Region Masking
            material.SetVector(RegionCenterID, new Vector4(regionCenter.x, regionCenter.y, 0, 0));
            material.SetFloat(RegionRadiusID, regionRadius);
            material.SetFloat(RegionFalloffID, regionFalloff);
            material.SetFloat(RegionEdgeModeID, regionEdgeMode);

            // Per-layer fabric sampling with color distribution
            ApplyFabricPropertiesWithColors(material, fabricColorBlend);
        }

        /// <summary>
        /// Apply fabric properties including color palette to the material.
        /// </summary>
        protected void ApplyFabricPropertiesWithColors(Material material, float colorBlend)
        {
            var bridge = WorldFabricBridge.Instance;
            if (bridge == null)
            {
                // Fallback to standard fabric properties without colors
                ApplyFabricProperties(material);
                return;
            }

            bridge.ApplyFabricToMaterialWithColors(material, parallaxDepth, colorBlend);
        }
    }
}
