using System;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using Starfire.Core.V2.World.Sampling;
using StarfireV2;

namespace Starfire.Core.V2.World.Generation.Generators
{
    /// <summary>
    /// Generates nebula regions based on World Fabric SpaceZoneLayer data.
    /// Only creates nebulas in NebulaDense zones, ensuring visual alignment
    /// between procedural zone classification and rendered nebulas.
    /// </summary>
    public class FabricNebulaChunkGenerator : IChunkDataGenerator
    {
        private readonly FabricNebulaGenerationConfig _config;
        private SpaceZoneQuery _zoneQuery;
        private float _cachedWorldSeed;
        private const int GENERATOR_INDEX = 11; // Slightly different from legacy (10) to avoid seed collision

        public int Priority => 10; // Environment priority (same as legacy)
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(NebulaChunkData);

        public FabricNebulaChunkGenerator(FabricNebulaGenerationConfig config)
        {
            _config = config;
        }

        public void Generate(Chunk.Chunk chunk, ChunkGenerationContext context)
        {
            var data = new NebulaChunkData();

            if (_config == null || _config.nebulaConfigs.Count == 0)
            {
                chunk.SetData(data);
                return;
            }

            // Ensure we have a zone query
            EnsureZoneQuery(context.WorldSeed);

            if (_zoneQuery == null)
            {
                // WorldFabricService not available - fall back to empty data
                chunk.SetData(data);
                return;
            }

            var rng = context.GetGeneratorRandom(GENERATOR_INDEX);

            // Sample zone at chunk center for quick early-out check
            Vector2D chunkCenter = chunk.Coord.ToAbsoluteCenter(context.ChunkSize);
            var centerZone = _zoneQuery.QueryAt(chunkCenter, context.WorldSeed);

            // Early exit if chunk center is not in NebulaDense zone
            if (_config.requireNebulaDenseZone && centerZone.ZoneType != SpaceZoneType.NebulaDense)
            {
                chunk.SetData(data);
                return;
            }

            // Early exit if density is below threshold
            if (centerZone.Density < _config.minZoneDensity)
            {
                chunk.SetData(data);
                return;
            }

            // Use Poisson disk sampling for natural distribution
            Rect chunkBounds = chunk.AbsoluteBounds;
            var points = PoissonDiskSampler.Sample(
                chunkBounds,
                _config.minNebulaSpacing,
                rng,
                _config.maxSamplingAttempts
            );

            Vector2 chunkCenterV2 = chunk.GetAbsoluteCenter();

            foreach (var point in points)
            {
                // Query zone at this specific point
                var pointZone = _zoneQuery.QueryAt(
                    new Vector2D(point.x, point.y),
                    context.WorldSeed
                );

                // Skip if not in NebulaDense zone (when required)
                if (_config.requireNebulaDenseZone && pointZone.ZoneType != SpaceZoneType.NebulaDense)
                    continue;

                // Skip if density below threshold
                if (pointZone.Density < _config.minZoneDensity)
                    continue;

                // Density-based spawn probability
                float spawnProbability = _config.CalculateSpawnProbability(pointZone.Density);
                if ((float)rng.NextDouble() > spawnProbability)
                    continue;

                // Select config based on zone density
                var nebulaConfig = _config.SelectConfig(rng, pointZone.Density);
                if (nebulaConfig == null)
                    continue;

                // Calculate radius based on zone density
                float radius = _config.CalculateRadius(rng, pointZone.Density);

                // Local position relative to chunk center
                Vector2 localPosition = point - chunkCenterV2;

                // Generate unique seed for this nebula's patterns
                float nebulaSeed = (float)rng.NextDouble() * 10000f;

                var definition = new NebulaRegionDefinition(
                    localPosition,
                    radius,
                    nebulaConfig,
                    nebulaSeed
                );

                data.Regions.Add(definition);
            }

            chunk.SetData(data);
        }

        public void OnChunkUnloading(Chunk.Chunk chunk)
        {
            // No cleanup needed - NebulaRegionConsumer handles runtime objects
        }

        /// <summary>
        /// Ensure we have a valid zone query from WorldFabricService.
        /// </summary>
        private void EnsureZoneQuery(float worldSeed)
        {
            // Re-query if world seed changed (new world)
            if (_zoneQuery != null && Mathf.Approximately(_cachedWorldSeed, worldSeed))
                return;

            var fabricService = WorldFabricService.Instance;
            if (fabricService == null)
            {
                _zoneQuery = null;
                return;
            }

            // Ensure edit-mode queries are available (handles both edit and play mode)
            fabricService.EnsureEditModeQueries();
            _zoneQuery = fabricService.GetSpaceZoneQuery();
            _cachedWorldSeed = worldSeed;
        }
    }
}
