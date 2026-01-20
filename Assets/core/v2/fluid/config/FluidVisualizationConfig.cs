using UnityEngine;

namespace StarfireV2.Fluid
{
    /// <summary>
    /// Configuration for the fluid wake visualization.
    /// </summary>
    [CreateAssetMenu(fileName = "FluidVisualizationConfig", menuName = "Starfire/Fluid/Visualization Config")]
    public class FluidVisualizationConfig : ScriptableObject
    {
        [Header("Color Gradient")]
        [Tooltip("Color for low density areas (usually transparent)")]
        public Color lowDensityColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Color for mid density areas")]
        public Color midDensityColor = new Color(0.2f, 0.4f, 0.8f, 0.3f);

        [Tooltip("Color for high density areas")]
        public Color highDensityColor = new Color(0.5f, 0.8f, 1f, 0.6f);

        [Tooltip("Emission/glow intensity")]
        [Range(0f, 3f)]
        public float emissionIntensity = 1f;

        [Header("Density Display")]
        [Tooltip("Multiplier for density visualization")]
        [Range(0f, 5f)]
        public float densityMultiplier = 1f;

        [Tooltip("Multiplier for alpha/transparency")]
        [Range(0f, 2f)]
        public float alphaMultiplier = 1f;

        [Tooltip("Minimum density threshold to display")]
        [Range(0f, 0.5f)]
        public float alphaThreshold = 0.15f;

        [Header("Visual Noise (FBM Detail)")]
        [Tooltip("Scale of detail noise (smaller = larger patterns)")]
        [Range(0.01f, 1f)]
        public float visualNoiseScale = 0.15f;

        [Tooltip("Contrast of the noise detail")]
        [Range(0.1f, 3f)]
        public float visualNoiseContrast = 1.5f;

        [Tooltip("Number of noise octaves (more = finer detail)")]
        [Range(1, 6)]
        public int visualNoiseOctaves = 4;

        [Tooltip("Amplitude persistence between octaves")]
        [Range(0.3f, 0.7f)]
        public float visualNoisePersistence = 0.5f;

        [Header("Nebula Masking")]
        [Tooltip("Enable masking to only show wakes inside nebula regions")]
        public bool enableNebulaMask = true;

        [Tooltip("Softness of the nebula mask edge blending")]
        [Range(0f, 50f)]
        public float nebulaMaskSoftness = 10f;

        [Header("Debug")]
        [Tooltip("Show velocity field visualization instead of density")]
        public bool showVelocityVisualization = false;

        [Tooltip("Scale for velocity color visualization")]
        [Range(0f, 10f)]
        public float velocityColorScale = 1f;
    }
}
