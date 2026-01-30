using UnityEngine;
using Starfire.Core.Background.Layers;

namespace Starfire.Core.Background.Presets
{
    /// <summary>
    /// ScriptableObject preset for GalaxyLayer configuration.
    /// Assign to a GalaxyLayer to override all its settings.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGalaxyPreset", menuName = "Starfire/Presets/Galaxy Preset")]
    public class GalaxyLayerPreset : ScriptableObject
    {
        [Header("Layer Settings")]
        public float parallaxDepth = 0.001f;

        [Header("Galaxy Placement")]
        [Range(1f, 20f)]
        public float galaxyCellSize = 8f;
        [Range(0f, 1f)]
        public float galaxySpawnChance = 0.3f;
        [Range(0.05f, 2f)]
        public float galaxySizeMin = 0.3f;
        [Range(0.1f, 4f)]
        public float galaxySizeMax = 1.0f;

        [Header("Spiral Structure")]
        [Range(1f, 6f)]
        public float armCount = 2f;
        [Range(0.1f, 3f)]
        public float armWinding = 0.8f;
        [Range(0.05f, 1f)]
        public float armSpread = 0.3f;
        [Range(0.5f, 5f)]
        public float armFalloff = 2f;

        [Header("Core")]
        [Range(0.01f, 0.5f)]
        public float coreSize = 0.12f;
        [Range(0.5f, 5f)]
        public float coreBrightness = 2.5f;
        [Range(1f, 8f)]
        public float coreFalloff = 3f;

        [Header("Star Population")]
        [Range(20f, 400f)]
        public float starDensity = 120f;
        [Range(0.1f, 5f)]
        public float starBrightness = 1.5f;
        [Range(0.01f, 0.2f)]
        public float starSizeMin = 0.05f;
        [Range(0.05f, 0.8f)]
        public float starSizeMax = 0.3f;
        [Range(0.5f, 5f)]
        public float starFalloff = 1.5f;
        [Range(1f, 15f)]
        public float starConcentration = 5f;

        [Header("Bright Stars and Supergiants")]
        [Range(0f, 0.15f)]
        public float supergiantChance = 0.03f;
        [Range(0.1f, 1.5f)]
        public float supergiantSize = 0.6f;
        [Range(1f, 8f)]
        public float supergiantBrightness = 4f;
        [Range(0f, 0.5f)]
        public float spikeLength = 0.15f;
        [Range(0.001f, 0.05f)]
        public float spikeWidth = 0.008f;

        [Header("Star Color Variation")]
        [Range(0f, 1f)]
        public float blueStarChance = 0.3f;
        [Range(0f, 1f)]
        public float redStarChance = 0.15f;
        [Range(0f, 1f)]
        public float yellowStarChance = 0.25f;
        public Color colorBlue = new Color(0.6f, 0.7f, 1f);
        public Color colorRed = new Color(1f, 0.5f, 0.3f);
        public Color colorYellow = new Color(1f, 0.95f, 0.7f);
        public Color colorWhite = Color.white;

        [Header("Dust and Gas")]
        [Range(0f, 2f)]
        public float dustIntensity = 0.5f;
        [Range(1f, 10f)]
        public float dustNoiseScale = 4f;
        [Range(0f, 1f)]
        public float dustWarpStrength = 0.3f;

        [Header("Halo")]
        [Range(0.1f, 2f)]
        public float haloSize = 0.8f;
        [Range(0f, 1f)]
        public float haloIntensity = 0.15f;
        [Range(1f, 6f)]
        public float haloFalloff = 2.5f;

        [Header("Galaxy Color")]
        public Color colorCore = new Color(1f, 0.95f, 0.8f);
        public Color colorMid = new Color(0.6f, 0.7f, 1f);
        public Color colorOuter = new Color(0.3f, 0.35f, 0.6f);
        [Range(0.1f, 3f)]
        public float overallBrightness = 1f;
        [Range(0f, 1f)]
        public float opacity = 1f;

        [Header("Variation")]
        [Range(0f, 0.6f)]
        public float ellipticityRange = 0.3f;
        [Range(0f, 1f)]
        public float tiltRange = 1f;

        [Header("Zoom")]
        [Range(0f, 1f)]
        public float zoomResponse = 0.5f;

        [Header("Background")]
        public bool renderBackground = false;
        public Color backgroundColor = Color.black;

        [Header("Seed")]
        public float seed = 0f;

        public void ApplyTo(GalaxyLayer layer)
        {
            layer.parallaxDepth = parallaxDepth;
            layer.galaxyCellSize = galaxyCellSize;
            layer.galaxySpawnChance = galaxySpawnChance;
            layer.galaxySizeMin = galaxySizeMin;
            layer.galaxySizeMax = galaxySizeMax;
            layer.armCount = armCount;
            layer.armWinding = armWinding;
            layer.armSpread = armSpread;
            layer.armFalloff = armFalloff;
            layer.coreSize = coreSize;
            layer.coreBrightness = coreBrightness;
            layer.coreFalloff = coreFalloff;
            layer.starDensity = starDensity;
            layer.starBrightness = starBrightness;
            layer.starSizeMin = starSizeMin;
            layer.starSizeMax = starSizeMax;
            layer.starFalloff = starFalloff;
            layer.starConcentration = starConcentration;
            layer.supergiantChance = supergiantChance;
            layer.supergiantSize = supergiantSize;
            layer.supergiantBrightness = supergiantBrightness;
            layer.spikeLength = spikeLength;
            layer.spikeWidth = spikeWidth;
            layer.blueStarChance = blueStarChance;
            layer.redStarChance = redStarChance;
            layer.yellowStarChance = yellowStarChance;
            layer.colorBlue = colorBlue;
            layer.colorRed = colorRed;
            layer.colorYellow = colorYellow;
            layer.colorWhite = colorWhite;
            layer.dustIntensity = dustIntensity;
            layer.dustNoiseScale = dustNoiseScale;
            layer.dustWarpStrength = dustWarpStrength;
            layer.haloSize = haloSize;
            layer.haloIntensity = haloIntensity;
            layer.haloFalloff = haloFalloff;
            layer.colorCore = colorCore;
            layer.colorMid = colorMid;
            layer.colorOuter = colorOuter;
            layer.overallBrightness = overallBrightness;
            layer.opacity = opacity;
            layer.ellipticityRange = ellipticityRange;
            layer.tiltRange = tiltRange;
            layer.zoomResponse = zoomResponse;
            layer.renderBackground = renderBackground;
            layer.backgroundColor = backgroundColor;
            layer.seed = seed;
        }
    }
}
