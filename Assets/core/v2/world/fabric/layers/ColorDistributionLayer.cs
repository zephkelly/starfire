using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace StarfireV2
{
    /// <summary>
    /// World Fabric layer that generates procedural color palettes across the world.
    /// Uses noise fields for hue distribution with color harmony rules to ensure
    /// aesthetically pleasing nebula colors that vary by region.
    /// </summary>
    public class ColorDistributionLayer : IWorldLayer
    {
        public string LayerId => "ColorDistribution";
        public int Priority => 3;  // After SpaceZones (0), before Resources (5)
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(ColorDistributionLayerData);
        public IReadOnlyList<Type> Dependencies => new[] { typeof(SpaceZoneLayerData) };

        private readonly ColorDistributionConfig _config;
        private readonly PerlinNoiseField _hueField;
        private readonly PerlinNoiseField _detailField;
        private readonly float _seedOffset;

        public ColorDistributionLayer(ColorDistributionConfig config)
        {
            _config = config;
            _seedOffset = 31111f;  // Unique seed offset for color layer

            // Main hue field - large scale regional color variation
            _hueField = new PerlinNoiseField(
                1f / config.hueNoiseScale,
                octaves: 3,
                persistence: 0.5f
            );

            // Detail field - fine variation within regions
            _detailField = new PerlinNoiseField(
                1f / config.hueDetailScale,
                octaves: 2,
                persistence: 0.4f
            );
        }

        public WorldLayerData Generate(Chunk chunk, WorldFabricContext context)
        {
            Vector2D center = chunk.Coord.ToAbsoluteCenter(context.ChunkSize);

            // Get zone data for modifiers
            var zoneData = context.GetLayerData<SpaceZoneLayerData>();
            var zoneSample = zoneData?.FabricSample ?? default;

            // Sample the color distribution
            var colorSample = SampleColorDistribution(center, context.WorldSeed, zoneSample);

            return new ColorDistributionLayerData
            {
                Palette = colorSample.Palette,
                BaseHue = colorSample.BaseHue,
                HarmonyMode = _config.defaultHarmonyMode,
            };
        }

        public IWorldLayerQuery CreateQuery() => new ColorDistributionQuery(_config, this);

        public void OnChunkUnloading(Chunk chunk) { }

        /// <summary>
        /// Sample the color distribution at a world position.
        /// </summary>
        internal ColorDistributionSample SampleColorDistribution(
            Vector2D position,
            float worldSeed,
            SpaceFabricSample zoneSample)
        {
            // Sample base hue from noise field (0-1)
            float baseHue = _hueField.Sample(position, worldSeed + _seedOffset);

            // Add detail variation
            float detail = _detailField.Sample(position, worldSeed + _seedOffset + 1000f);
            baseHue += (detail - 0.5f) * _config.hueDetailStrength * 2f;
            baseHue = Mathf.Repeat(baseHue, 1f);

            // Determine harmony mode (default or zone-specific)
            ColorHarmonyMode harmonyMode = _config.defaultHarmonyMode;

            // Generate base palette from procedural hue
            ColorPalette palette = ColorPalette.FromBaseHue(
                baseHue,
                harmonyMode,
                _config.baseSaturationMultiplier,
                _config.baseValueMultiplier
            );

            // Apply zone modifiers
            float nebulaMod = zoneSample.NebulaDensity * _config.nebulaSaturationInfluence;
            float voidMod = zoneSample.VoidFactor * _config.voidDesaturationInfluence;
            float anomalyMod = zoneSample.AnomalyStrength * _config.anomalyHueShiftInfluence;

            palette = palette.ApplyZoneModifiers(nebulaMod, voidMod, anomalyMod);

            // Check for authored palette overrides
            SpaceZoneType zoneType = SpaceZoneLayer.DerivePrimaryZone(zoneSample);
            if (_config.TryGetOverrideForZone(zoneType, out var paletteOverride))
            {
                float zoneDensity = GetZoneDensity(zoneSample, zoneType);
                if (zoneDensity >= paletteOverride.densityThreshold)
                {
                    // Blend toward authored palette based on density and override strength
                    float blendFactor = Mathf.InverseLerp(
                        paletteOverride.densityThreshold,
                        1f,
                        zoneDensity
                    ) * paletteOverride.overrideStrength;

                    ColorPalette authoredPalette = paletteOverride.palette.ToColorPalette();
                    palette = ColorPalette.Lerp(palette, authoredPalette, blendFactor);
                }
            }

            return new ColorDistributionSample
            {
                Palette = palette,
                BaseHue = baseHue,
                HarmonyMode = harmonyMode,
            };
        }

        private float GetZoneDensity(SpaceFabricSample sample, SpaceZoneType zoneType)
        {
            return zoneType switch
            {
                SpaceZoneType.NebulaDense => sample.NebulaDensity,
                SpaceZoneType.AsteroidBelt => sample.AsteroidDensity,
                SpaceZoneType.DeepVoid => sample.VoidFactor,
                SpaceZoneType.Anomaly => sample.AnomalyStrength,
                _ => sample.OverallDensity,
            };
        }
    }

    /// <summary>
    /// Per-chunk color distribution data.
    /// </summary>
    public class ColorDistributionLayerData : WorldLayerData
    {
        public override string LayerId => "ColorDistribution";

        /// <summary>The sampled palette at this chunk's center.</summary>
        public ColorPalette Palette;

        /// <summary>The procedural base hue (0-1).</summary>
        public float BaseHue;

        /// <summary>The harmony mode used for generation.</summary>
        public ColorHarmonyMode HarmonyMode;
    }

    /// <summary>
    /// Runtime query for color distribution at any world position.
    /// </summary>
    public class ColorDistributionQuery : IWorldLayerQuery<ColorDistributionSample>
    {
        private readonly ColorDistributionConfig _config;
        private readonly ColorDistributionLayer _layer;

        // Cache for zone query (needed for zone modifiers)
        private SpaceZoneQuery _zoneQuery;

        public ColorDistributionQuery(ColorDistributionConfig config, ColorDistributionLayer layer)
        {
            _config = config;
            _layer = layer;
        }

        /// <summary>
        /// Set the zone query for zone-aware color sampling.
        /// Called by WorldFabricService when registering.
        /// </summary>
        public void SetZoneQuery(SpaceZoneQuery zoneQuery)
        {
            _zoneQuery = zoneQuery;
        }

        public ColorDistributionSample QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            // Get zone sample if query available
            SpaceFabricSample zoneSample = default;
            if (_zoneQuery != null)
            {
                var zoneResult = _zoneQuery.QueryAt(absolutePosition, worldSeed);
                zoneSample = zoneResult.FabricSample;
            }

            return _layer.SampleColorDistribution(absolutePosition, worldSeed, zoneSample);
        }

        object IWorldLayerQuery.QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            return QueryAt(absolutePosition, worldSeed);
        }
    }

    /// <summary>
    /// Result of a color distribution sample at a position.
    /// </summary>
    public struct ColorDistributionSample
    {
        /// <summary>The 4-color palette at this position.</summary>
        public ColorPalette Palette;

        /// <summary>The base procedural hue (0-1).</summary>
        public float BaseHue;

        /// <summary>The harmony mode used.</summary>
        public ColorHarmonyMode HarmonyMode;

        public static ColorDistributionSample Default => new ColorDistributionSample
        {
            Palette = ColorPalette.Default,
            BaseHue = 0.75f,  // Default purple hue
            HarmonyMode = ColorHarmonyMode.Analogous,
        };
    }
}
