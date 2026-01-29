using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for the Color Distribution fabric layer.
    /// Controls procedural color generation and authored palette overrides.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorDistributionConfig", menuName = "Starfire/World Fabric/Color Distribution Config")]
    public class ColorDistributionConfig : ScriptableObject
    {
        [Header("Enable")]
        [Tooltip("Enable or disable color distribution generation")]
        public bool enabled = true;

        [Header("Procedural Hue Generation")]
        [Tooltip("Scale of the hue noise field. Larger = bigger regions of similar color")]
        [Range(10000f, 500000f)]
        public float hueNoiseScale = 100000f;

        [Tooltip("Secondary noise scale for fine detail variation")]
        [Range(5000f, 50000f)]
        public float hueDetailScale = 20000f;

        [Tooltip("How much the detail noise influences the final hue (0-1)")]
        [Range(0f, 0.5f)]
        public float hueDetailStrength = 0.15f;

        [Header("Color Harmony")]
        [Tooltip("Default color harmony mode for procedural generation")]
        public ColorHarmonyMode defaultHarmonyMode = ColorHarmonyMode.Analogous;

        [Tooltip("Saturation multiplier for all generated colors")]
        [Range(0.5f, 2f)]
        public float baseSaturationMultiplier = 1f;

        [Tooltip("Value/brightness multiplier for all generated colors")]
        [Range(0.5f, 2f)]
        public float baseValueMultiplier = 1f;

        [Header("Zone Influence")]
        [Tooltip("How much nebula density affects saturation (0 = none, 1 = full)")]
        [Range(0f, 1f)]
        public float nebulaSaturationInfluence = 0.5f;

        [Tooltip("How much void factor desaturates and cools colors")]
        [Range(0f, 1f)]
        public float voidDesaturationInfluence = 0.7f;

        [Tooltip("How much anomaly strength shifts colors toward red")]
        [Range(0f, 1f)]
        public float anomalyHueShiftInfluence = 0.5f;

        [Header("Transition Smoothing")]
        [Tooltip("Smoothing factor for color transitions. Higher = smoother but slower")]
        [Range(0.5f, 5f)]
        public float transitionSmoothness = 2f;

        [Header("Authored Palette Overrides")]
        [Tooltip("Predefined palettes that can override procedural generation for specific zones")]
        public List<ZonePaletteOverride> paletteOverrides = new List<ZonePaletteOverride>();

        /// <summary>
        /// Get authored palette for a zone type, if one exists.
        /// </summary>
        public bool TryGetOverrideForZone(SpaceZoneType zoneType, out ZonePaletteOverride paletteOverride)
        {
            foreach (var ovr in paletteOverrides)
            {
                if (ovr.targetZoneType == zoneType && ovr.enabled)
                {
                    paletteOverride = ovr;
                    return true;
                }
            }
            paletteOverride = default;
            return false;
        }

        /// <summary>
        /// Get the MetaballField configuration for hue distribution.
        /// Note: We use PerlinNoiseField instead for smoother gradients.
        /// </summary>
        public PerlinNoiseFieldConfig GetHueFieldConfig()
        {
            return new PerlinNoiseFieldConfig
            {
                scale = hueNoiseScale,
                octaves = 3,
                persistence = 0.5f,
                lacunarity = 2f,
            };
        }

        /// <summary>
        /// Get the detail noise configuration.
        /// </summary>
        public PerlinNoiseFieldConfig GetDetailFieldConfig()
        {
            return new PerlinNoiseFieldConfig
            {
                scale = hueDetailScale,
                octaves = 2,
                persistence = 0.4f,
                lacunarity = 2.5f,
            };
        }
    }

    /// <summary>
    /// An authored palette override for a specific zone type.
    /// </summary>
    [Serializable]
    public class ZonePaletteOverride
    {
        [Tooltip("Enable this override")]
        public bool enabled = true;

        [Tooltip("Which zone type this palette applies to")]
        public SpaceZoneType targetZoneType = SpaceZoneType.NebulaDense;

        [Tooltip("The palette to use for this zone")]
        public AuthoredPalette palette;

        [Tooltip("How strongly this overrides procedural colors (0 = blend, 1 = full override)")]
        [Range(0f, 1f)]
        public float overrideStrength = 0.8f;

        [Tooltip("Zone density threshold before this override applies")]
        [Range(0f, 1f)]
        public float densityThreshold = 0.3f;
    }

    /// <summary>
    /// A manually authored 4-color palette.
    /// </summary>
    [Serializable]
    public class AuthoredPalette
    {
        [Tooltip("Name for identification")]
        public string name = "Custom Palette";

        [Tooltip("Outer/faint color")]
        public Color color1 = new Color(0.1f, 0.05f, 0.2f, 1f);

        [Tooltip("Mid-tone color")]
        public Color color2 = new Color(0.4f, 0.1f, 0.3f, 1f);

        [Tooltip("Brighter color")]
        public Color color3 = new Color(0.8f, 0.3f, 0.4f, 1f);

        [Tooltip("Core/brightest color")]
        public Color color4 = new Color(1f, 0.8f, 0.6f, 1f);

        public ColorPalette ToColorPalette()
        {
            return new ColorPalette
            {
                Color1 = color1,
                Color2 = color2,
                Color3 = color3,
                Color4 = color4,
            };
        }
    }

    /// <summary>
    /// Configuration for Perlin noise-based fields (used for smooth hue gradients).
    /// </summary>
    [Serializable]
    public struct PerlinNoiseFieldConfig
    {
        public float scale;
        public int octaves;
        public float persistence;
        public float lacunarity;
    }
}
