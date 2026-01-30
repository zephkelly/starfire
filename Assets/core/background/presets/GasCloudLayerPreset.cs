using UnityEngine;
using Starfire.Core.Background.Layers;

namespace Starfire.Core.Background.Presets
{
    /// <summary>
    /// ScriptableObject preset for GasCloudLayer configuration.
    /// Assign to a GasCloudLayer to override all its settings.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGasCloudPreset", menuName = "Starfire/Presets/Gas Cloud Preset")]
    public class GasCloudLayerPreset : ScriptableObject
    {
        [Header("Layer Settings")]
        [Tooltip("Parallax depth - lower values = farther/slower")]
        public float parallaxDepth = 0.02f;

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

        /// <summary>
        /// Apply all preset values to the given layer.
        /// </summary>
        public void ApplyTo(GasCloudLayer layer)
        {
            layer.parallaxDepth = parallaxDepth;
            layer.noiseScale = noiseScale;
            layer.octaves = octaves;
            layer.persistence = persistence;
            layer.lacunarity = lacunarity;
            layer.warpStrength = warpStrength;
            layer.warpScale = warpScale;
            layer.colorCount = colorCount;
            layer.color1 = color1;
            layer.color2 = color2;
            layer.color3 = color3;
            layer.color4 = color4;
            layer.gradientBias = gradientBias;
            layer.gradientContrast = gradientContrast;
            layer.emissionIntensity = emissionIntensity;
            layer.coreEmissionBoost = coreEmissionBoost;
            layer.density = density;
            layer.edgeSoftness = edgeSoftness;
            layer.threshold = threshold;
            layer.detailFrequency = detailFrequency;
            layer.renderBackground = renderBackground;
            layer.backgroundColor = backgroundColor;
            layer.seed = seed;
        }
    }
}
