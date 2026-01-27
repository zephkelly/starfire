using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace StarfireV2
{
    // =========================================================================
    // Bitmask flags for overlapping space properties
    // =========================================================================

    [Flags]
    public enum SpaceProperty
    {
        None      = 0,
        Void      = 1 << 0,
        Nebula    = 1 << 1,
        Asteroids = 1 << 2,
        Anomaly   = 1 << 3,
    }

    /// <summary>
    /// Rich sample of all space properties at a given point.
    /// Each property is a continuous 0-1 density, and the bitmask
    /// indicates which properties exceed their activation threshold.
    /// </summary>
    public struct SpaceFabricSample
    {
        public SpaceProperty ActiveProperties;
        public float NebulaDensity;
        public float AsteroidDensity;
        public float VoidFactor;
        public float AnomalyStrength;

        /// <summary>
        /// Overall "how active is this area" value for legacy compatibility.
        /// High in nebula/asteroid regions, low in voids.
        /// </summary>
        public float OverallDensity => Mathf.Clamp01(
            Mathf.Max(NebulaDensity, AsteroidDensity) * (1f - VoidFactor * 0.7f)
        );

        public bool HasProperty(SpaceProperty prop) => (ActiveProperties & prop) != 0;
    }

    // =========================================================================
    // Legacy enum kept for backward compatibility
    // =========================================================================

    public enum SpaceZoneType
    {
        DeepVoid,
        SparseSpace,
        OpenSpace,
        AsteroidBelt,
        NebulaDense,
        Anomaly
    }

    // =========================================================================
    // SpaceZoneLayer — uses MetaballFields for bubble-like regions
    // =========================================================================

    public class SpaceZoneLayer : IWorldLayer
    {
        public string LayerId => "SpaceZones";
        public int Priority => 0;
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(SpaceZoneLayerData);
        public IReadOnlyList<Type> Dependencies => Array.Empty<Type>();

        private readonly SpaceZoneConfig _config;
        private readonly MetaballField _nebulaField;
        private readonly MetaballField _asteroidField;
        private readonly MetaballField _voidField;
        private readonly MetaballField _anomalyField;

        public SpaceZoneLayer(SpaceZoneConfig config)
        {
            _config = config;

            // Each property gets its own metaball field with a unique seed offset
            _nebulaField = new MetaballField(config.GetNebulaFieldConfig(), seedOffset: 0f);
            _asteroidField = new MetaballField(config.GetAsteroidFieldConfig(), seedOffset: 7777f);
            _voidField = new MetaballField(config.GetVoidFieldConfig(), seedOffset: 15555f);
            _anomalyField = new MetaballField(config.GetAnomalyFieldConfig(), seedOffset: 23333f);
        }

        public WorldLayerData Generate(Chunk chunk, WorldFabricContext context)
        {
            Vector2D center = chunk.Coord.ToAbsoluteCenter(context.ChunkSize);
            var sample = SampleFabric(center, context.WorldSeed);

            var data = new SpaceZoneLayerData();
            data.PrimaryZoneType = DerivePrimaryZone(sample);
            data.ZoneDensity = sample.OverallDensity;
            data.ZoneVariation = sample.NebulaDensity;
            data.FabricSample = sample;

            return data;
        }

        public IWorldLayerQuery CreateQuery() => new SpaceZoneQuery(_config, this);

        public void OnChunkUnloading(Chunk chunk) { }

        /// <summary>
        /// Sample all space properties at a position. Returns continuous densities
        /// and a bitmask of which properties are active.
        /// </summary>
        internal SpaceFabricSample SampleFabric(Vector2D position, float worldSeed)
        {
            var nebula = _nebulaField.SampleDetailed(position, worldSeed);
            var asteroid = _asteroidField.SampleDetailed(position, worldSeed);
            var voidResult = _voidField.SampleDetailed(position, worldSeed);
            var anomaly = _anomalyField.SampleDetailed(position, worldSeed);

            var props = SpaceProperty.None;
            if (voidResult.IsActive) props |= SpaceProperty.Void;
            if (nebula.IsActive) props |= SpaceProperty.Nebula;
            if (asteroid.IsActive) props |= SpaceProperty.Asteroids;
            if (anomaly.IsActive) props |= SpaceProperty.Anomaly;

            return new SpaceFabricSample
            {
                ActiveProperties = props,
                NebulaDensity = nebula.NormalizedDensity,
                AsteroidDensity = asteroid.NormalizedDensity,
                VoidFactor = voidResult.NormalizedDensity,
                AnomalyStrength = anomaly.NormalizedDensity,
            };
        }

        /// <summary>
        /// Derive a single legacy SpaceZoneType from the fabric sample.
        /// Uses the dominant property as the primary zone.
        /// </summary>
        internal static SpaceZoneType DerivePrimaryZone(SpaceFabricSample sample)
        {
            // Anomaly takes priority when active (rare, important)
            if (sample.HasProperty(SpaceProperty.Anomaly) && sample.AnomalyStrength > 0.3f)
                return SpaceZoneType.Anomaly;

            // Deep void when void is dominant and nothing else is strongly present
            if (sample.HasProperty(SpaceProperty.Void) && sample.VoidFactor > 0.5f
                && sample.NebulaDensity < 0.2f && sample.AsteroidDensity < 0.2f)
                return SpaceZoneType.DeepVoid;

            // Nebula when nebula density is dominant
            if (sample.HasProperty(SpaceProperty.Nebula) && sample.NebulaDensity > sample.AsteroidDensity)
                return SpaceZoneType.NebulaDense;

            // Asteroid belt when asteroid density is dominant
            if (sample.HasProperty(SpaceProperty.Asteroids))
                return SpaceZoneType.AsteroidBelt;

            // Sparse space: some void influence but not deep void
            if (sample.VoidFactor > 0.15f)
                return SpaceZoneType.SparseSpace;

            return SpaceZoneType.OpenSpace;
        }

        // Legacy convenience for old callers
        internal ZoneSampleResult SampleZone(Vector2D position, float worldSeed)
        {
            var sample = SampleFabric(position, worldSeed);
            return new ZoneSampleResult
            {
                ZoneType = DerivePrimaryZone(sample),
                Density = sample.OverallDensity,
                ZoneValue = sample.NebulaDensity,
            };
        }
    }

    internal struct ZoneSampleResult
    {
        public SpaceZoneType ZoneType;
        public float Density;
        public float ZoneValue;
    }

    // =========================================================================
    // Layer data stored per chunk
    // =========================================================================

    public class SpaceZoneLayerData : WorldLayerData
    {
        public override string LayerId => "SpaceZones";

        /// <summary>Legacy single zone type (derived from dominant property).</summary>
        public SpaceZoneType PrimaryZoneType;

        /// <summary>Overall density (0-1).</summary>
        public float ZoneDensity;

        /// <summary>Nebula density for legacy consumers.</summary>
        public float ZoneVariation;

        /// <summary>Full fabric sample with all property densities and bitmask.</summary>
        public SpaceFabricSample FabricSample;
    }

    // =========================================================================
    // Query classes for runtime position lookups
    // =========================================================================

    public class SpaceZoneQuery : IWorldLayerQuery<SpaceZoneQueryResult>
    {
        private readonly SpaceZoneConfig _config;
        private readonly SpaceZoneLayer _layer;

        public SpaceZoneQuery(SpaceZoneConfig config, SpaceZoneLayer layer)
        {
            _config = config;
            _layer = layer;
        }

        public SpaceZoneQueryResult QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            var sample = _layer.SampleFabric(absolutePosition, worldSeed);

            return new SpaceZoneQueryResult
            {
                ZoneType = SpaceZoneLayer.DerivePrimaryZone(sample),
                Density = sample.OverallDensity,
                FabricSample = sample,
            };
        }

        object IWorldLayerQuery.QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            return QueryAt(absolutePosition, worldSeed);
        }
    }

    public struct SpaceZoneQueryResult
    {
        /// <summary>Legacy single zone type.</summary>
        public SpaceZoneType ZoneType;

        /// <summary>Overall density.</summary>
        public float Density;

        /// <summary>Full fabric sample with all property densities.</summary>
        public SpaceFabricSample FabricSample;
    }
}
