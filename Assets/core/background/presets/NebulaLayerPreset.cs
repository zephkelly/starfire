using UnityEngine;
using Starfire.Core.Background.Layers;

namespace Starfire.Core.Background.Presets
{
    /// <summary>
    /// ScriptableObject preset for NebulaLayer configuration.
    /// Assign to a NebulaLayer to override all its settings.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNebulaPreset", menuName = "Starfire/Presets/Nebula Preset")]
    public class NebulaLayerPreset : ScriptableObject
    {
        [Header("Layer Settings")]
        [Tooltip("Parallax depth - lower values = farther/slower")]
        public float parallaxDepth = 0.02f;

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

        /// <summary>
        /// Apply all preset values to the given layer.
        /// </summary>
        public void ApplyTo(NebulaLayer layer)
        {
            layer.parallaxDepth = parallaxDepth;

            // Style Features
            layer.enablePillars = enablePillars;
            layer.enablePainterly = enablePainterly;
            layer.enableTendrils = enableTendrils;
            layer.enableCellular = enableCellular;

            // Pillar Settings
            layer.pillarStretch = pillarStretch;
            layer.pillarAngle = pillarAngle;
            layer.pillarWarpBias = pillarWarpBias;

            // Painterly Settings
            layer.posterizeLevels = posterizeLevels;
            layer.brushStrokeScale = brushStrokeScale;
            layer.brushWarpAmount = brushWarpAmount;

            // Tendril Settings
            layer.curlStrength = curlStrength;
            layer.curlScale = curlScale;
            layer.tendrilLength = tendrilLength;

            // Cellular Settings
            layer.voronoiScale = voronoiScale;
            layer.cellEdgeWidth = cellEdgeWidth;
            layer.bubbleInvert = bubbleInvert;

            // Internal Structure
            layer.enableDenseCores = enableDenseCores;
            layer.coreIntensity = coreIntensity;
            layer.haloSize = haloSize;
            layer.enableEdgeLit = enableEdgeLit;
            layer.rimLightStrength = rimLightStrength;
            layer.rimLightColor = rimLightColor;
            layer.enableDepthBands = enableDepthBands;
            layer.bandCount = bandCount;
            layer.bandContrast = bandContrast;
            layer.enableBrightSpots = enableBrightSpots;
            layer.spotDensity = spotDensity;
            layer.spotIntensity = spotIntensity;

            // Edge Style
            layer.edgeMode = edgeMode;
            layer.silhouetteSharpness = silhouetteSharpness;
            layer.internalSoftness = internalSoftness;

            // Base Noise
            layer.noiseScale = noiseScale;
            layer.octaves = octaves;
            layer.persistence = persistence;
            layer.lacunarity = lacunarity;
            layer.warpStrength = warpStrength;
            layer.warpScale = warpScale;

            // Color Gradient
            layer.colorCount = colorCount;
            layer.color1 = color1;
            layer.color2 = color2;
            layer.color3 = color3;
            layer.color4 = color4;
            layer.gradientBias = gradientBias;
            layer.gradientContrast = gradientContrast;

            // Emission
            layer.emissionIntensity = emissionIntensity;
            layer.coreEmissionBoost = coreEmissionBoost;

            // Density
            layer.density = density;
            layer.threshold = threshold;

            // Background
            layer.renderBackground = renderBackground;
            layer.backgroundColor = backgroundColor;

            // Seed
            layer.seed = seed;
        }
    }
}
