using System;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using Starfire.Core.V2.World.Sampling;

namespace Starfire.Core.V2.World.Generation.Generators
{
    /// <summary>
    /// Generates nebula regions within chunks using Poisson disk sampling.
    /// Creates NebulaChunkData with region definitions that consumers
    /// will use to create actual NebulaRegion instances.
    /// </summary>
    public class NebulaChunkGenerator : IChunkDataGenerator
    {
        private readonly NebulaGenerationConfig _config;
        private const int GENERATOR_INDEX = 10; // For seeding

        public int Priority => 10; // Environment priority
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(NebulaChunkData);

        public NebulaChunkGenerator(NebulaGenerationConfig config)
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

            // Get a seeded random for this generator
            var rng = context.GetGeneratorRandom(GENERATOR_INDEX);

            // Sample density at chunk center to determine if we should generate here
            Vector2 chunkCenter = chunk.GetAbsoluteCenter();
            float chunkDensity = SampleDensity(chunkCenter);

            if (chunkDensity < _config.minDensityThreshold)
            {
                // No nebulae in this chunk - still store empty data
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

            // Create nebula definitions for each point
            foreach (var point in points)
            {
                // Check density at this specific point
                float pointDensity = SampleDensity(point);
                if (pointDensity < _config.minDensityThreshold)
                    continue;

                // Density-based probability for spawning
                float spawnChance = Mathf.InverseLerp(_config.minDensityThreshold, 1f, pointDensity);
                if ((float)rng.NextDouble() > spawnChance)
                    continue;

                // Select config based on density
                var config = _config.SelectConfig(rng, pointDensity);
                if (config == null)
                    continue;

                // Calculate local position (relative to chunk center)
                Vector2 localPosition = point - chunkCenter;

                // Generate radius
                float radius = _config.SampleRadius(rng);

                // Generate unique seed for this nebula's patterns
                float nebulaSeed = (float)rng.NextDouble() * 10000f;

                var definition = new NebulaRegionDefinition(
                    localPosition,
                    radius,
                    config,
                    nebulaSeed
                );

                data.Regions.Add(definition);
            }

            chunk.SetData(data);
        }

        public void OnChunkUnloading(Chunk.Chunk chunk)
        {
            // No cleanup needed - consumer handles runtime objects
        }

        /// <summary>
        /// Sample the density field at a given position.
        /// Uses layered Perlin noise for natural variation.
        /// </summary>
        private float SampleDensity(Vector2 position)
        {
            float x = position.x * _config.densityNoiseScale + _config.densityNoiseOffset.x;
            float y = position.y * _config.densityNoiseScale + _config.densityNoiseOffset.y;

            // Layer multiple octaves for more interesting patterns
            float density = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < 3; i++)
            {
                density += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            // Normalize to 0-1
            return density / maxValue;
        }
    }
}
