using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace StarfireV2
{
    // =========================================================================
    // Bitmask flags for overlapping resource properties
    // =========================================================================

    [Flags]
    public enum ResourceProperty
    {
        None    = 0,
        Mineral = 1 << 0,
        Ore     = 1 << 1,
        Gas     = 1 << 2,
        Exotic  = 1 << 3,
        Water   = 1 << 4,
    }

    /// <summary>
    /// Rarity tier derived from overall resource value.
    /// </summary>
    public enum ResourceRarityTier
    {
        None,
        Common,
        Uncommon,
        Rare,
        Exotic,
    }

    /// <summary>
    /// Rich sample of all resource properties at a given point.
    /// Each resource is a continuous 0-1 density, and the bitmask
    /// indicates which resources exceed their activation threshold.
    /// </summary>
    public struct ResourceFabricSample
    {
        public ResourceProperty ActiveResources;
        public float MineralDensity;
        public float OreDensity;
        public float GasDensity;
        public float ExoticDensity;
        public float WaterDensity;

        /// <summary>
        /// Weighted overall resource value (exotic worth more).
        /// Set during sampling based on config weights.
        /// </summary>
        public float OverallResourceValue;

        /// <summary>
        /// Rarity tier derived from the overall resource value.
        /// </summary>
        public ResourceRarityTier RarityTier;

        public bool HasResource(ResourceProperty prop) => (ActiveResources & prop) != 0;
    }

    // =========================================================================
    // ResourceLayer — uses MetaballFields for resource distribution
    // =========================================================================

    public class ResourceLayer : IWorldLayer
    {
        public string LayerId => "Resources";
        public int Priority => 5;
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(ResourceLayerData);
        public IReadOnlyList<Type> Dependencies => _dependencies;

        private static readonly Type[] _dependencies = { typeof(SpaceZoneLayerData) };

        private readonly ResourceConfig _config;
        private readonly MetaballField _mineralField;
        private readonly MetaballField _oreField;
        private readonly MetaballField _gasField;
        private readonly MetaballField _exoticField;
        private readonly MetaballField _waterField;

        public ResourceLayer(ResourceConfig config)
        {
            _config = config;

            _mineralField = new MetaballField(config.GetMineralFieldConfig(), seedOffset: 30000f);
            _oreField = new MetaballField(config.GetOreFieldConfig(), seedOffset: 37777f);
            _gasField = new MetaballField(config.GetGasFieldConfig(), seedOffset: 45555f);
            _exoticField = new MetaballField(config.GetExoticFieldConfig(), seedOffset: 53333f);
            _waterField = new MetaballField(config.GetWaterFieldConfig(), seedOffset: 61111f);
        }

        public WorldLayerData Generate(Chunk chunk, WorldFabricContext context)
        {
            Vector2D center = chunk.Coord.ToAbsoluteCenter(context.ChunkSize);

            // Get space zone data for correlation boosts
            var zoneData = context.GetLayerData<SpaceZoneLayerData>();
            SpaceFabricSample zoneSample = zoneData?.FabricSample ?? default;

            var sample = SampleResources(center, context.WorldSeed, zoneSample);

            return new ResourceLayerData
            {
                FabricSample = sample,
            };
        }

        public IWorldLayerQuery CreateQuery() => new ResourceQuery(_config, this);

        public void OnChunkUnloading(Chunk chunk) { }

        /// <summary>
        /// Sample all resource properties at a position with zone correlation applied.
        /// </summary>
        internal ResourceFabricSample SampleResources(Vector2D position, float worldSeed, SpaceFabricSample zoneSample)
        {
            var mineral = _mineralField.SampleDetailed(position, worldSeed);
            var ore = _oreField.SampleDetailed(position, worldSeed);
            var gas = _gasField.SampleDetailed(position, worldSeed);
            var exotic = _exoticField.SampleDetailed(position, worldSeed);
            var water = _waterField.SampleDetailed(position, worldSeed);

            // Apply zone correlation boosts
            float mineralDensity = Mathf.Clamp01(mineral.NormalizedDensity * GetMineralBoost(zoneSample));
            float oreDensity = Mathf.Clamp01(ore.NormalizedDensity * GetOreBoost(zoneSample));
            float gasDensity = Mathf.Clamp01(gas.NormalizedDensity * GetGasBoost(zoneSample));
            float exoticDensity = Mathf.Clamp01(exotic.NormalizedDensity * GetExoticBoost(zoneSample));
            float waterDensity = Mathf.Clamp01(water.NormalizedDensity * GetWaterBoost(zoneSample));

            // Build active bitmask (using boosted densities against original thresholds)
            var props = ResourceProperty.None;
            if (mineralDensity >= _config.mineralThreshold) props |= ResourceProperty.Mineral;
            if (oreDensity >= _config.oreThreshold) props |= ResourceProperty.Ore;
            if (gasDensity >= _config.gasThreshold) props |= ResourceProperty.Gas;
            if (exoticDensity >= _config.exoticThreshold) props |= ResourceProperty.Exotic;
            if (waterDensity >= _config.waterThreshold) props |= ResourceProperty.Water;

            // Calculate weighted overall value
            float totalWeight = _config.mineralValueWeight + _config.oreValueWeight +
                                _config.gasValueWeight + _config.exoticValueWeight +
                                _config.waterValueWeight;

            float overallValue = (
                mineralDensity * _config.mineralValueWeight +
                oreDensity * _config.oreValueWeight +
                gasDensity * _config.gasValueWeight +
                exoticDensity * _config.exoticValueWeight +
                waterDensity * _config.waterValueWeight
            ) / totalWeight;

            return new ResourceFabricSample
            {
                ActiveResources = props,
                MineralDensity = mineralDensity,
                OreDensity = oreDensity,
                GasDensity = gasDensity,
                ExoticDensity = exoticDensity,
                WaterDensity = waterDensity,
                OverallResourceValue = overallValue,
                RarityTier = DeriveRarityTier(overallValue),
            };
        }

        /// <summary>
        /// Sample raw resources without zone correlation (for queries without zone context).
        /// </summary>
        internal ResourceFabricSample SampleResourcesRaw(Vector2D position, float worldSeed)
        {
            return SampleResources(position, worldSeed, default);
        }

        private float GetMineralBoost(SpaceFabricSample zone)
        {
            return zone.HasProperty(SpaceProperty.Asteroids) ? _config.mineralAsteroidBoost : 1f;
        }

        private float GetOreBoost(SpaceFabricSample zone)
        {
            return zone.HasProperty(SpaceProperty.Asteroids) ? _config.oreAsteroidBoost : 1f;
        }

        private float GetGasBoost(SpaceFabricSample zone)
        {
            return zone.HasProperty(SpaceProperty.Nebula) ? _config.gasNebulaBoost : 1f;
        }

        private float GetExoticBoost(SpaceFabricSample zone)
        {
            return zone.HasProperty(SpaceProperty.Anomaly) ? _config.exoticAnomalyBoost : 1f;
        }

        private float GetWaterBoost(SpaceFabricSample zone)
        {
            return zone.HasProperty(SpaceProperty.Nebula) ? _config.waterNebulaBoost : 1f;
        }

        private ResourceRarityTier DeriveRarityTier(float overallValue)
        {
            if (overallValue >= _config.exoticTierThreshold) return ResourceRarityTier.Exotic;
            if (overallValue >= _config.rareThreshold) return ResourceRarityTier.Rare;
            if (overallValue >= _config.uncommonThreshold) return ResourceRarityTier.Uncommon;
            if (overallValue >= _config.commonThreshold) return ResourceRarityTier.Common;
            return ResourceRarityTier.None;
        }
    }

    // =========================================================================
    // Layer data stored per chunk
    // =========================================================================

    public class ResourceLayerData : WorldLayerData
    {
        public override string LayerId => "Resources";

        /// <summary>Full resource fabric sample with all densities and bitmask.</summary>
        public ResourceFabricSample FabricSample;
    }

    // =========================================================================
    // Query class for runtime position lookups
    // =========================================================================

    public class ResourceQuery : IWorldLayerQuery<ResourceQueryResult>
    {
        private readonly ResourceConfig _config;
        private readonly ResourceLayer _layer;

        public ResourceQuery(ResourceConfig config, ResourceLayer layer)
        {
            _config = config;
            _layer = layer;
        }

        public ResourceQueryResult QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            // Runtime queries don't have zone context from the generation pipeline,
            // so we sample raw (no zone correlation boost).
            // For full correlation, callers should use WorldFabricService.SampleResourcesAt()
            // which can also sample zones.
            var sample = _layer.SampleResourcesRaw(absolutePosition, worldSeed);

            return new ResourceQueryResult
            {
                FabricSample = sample,
            };
        }

        /// <summary>
        /// Query with zone correlation applied. Used by WorldFabricService
        /// which can provide the zone sample.
        /// </summary>
        public ResourceQueryResult QueryWithZone(Vector2D absolutePosition, float worldSeed, SpaceFabricSample zoneSample)
        {
            var sample = _layer.SampleResources(absolutePosition, worldSeed, zoneSample);
            return new ResourceQueryResult
            {
                FabricSample = sample,
            };
        }

        object IWorldLayerQuery.QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            return QueryAt(absolutePosition, worldSeed);
        }
    }

    public struct ResourceQueryResult
    {
        /// <summary>Full resource fabric sample with all densities.</summary>
        public ResourceFabricSample FabricSample;
    }
}
