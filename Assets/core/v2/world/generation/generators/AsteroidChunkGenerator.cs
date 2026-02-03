using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using Starfire.Core.Noise;
using StarfireV2;

namespace Starfire.Core.V2.World.Generation.Generators
{
    /// <summary>
    /// Generates asteroid placements from three sources:
    /// 1. Belt: SpaceZoneLayer asteroid density regions
    /// 2. Ring: planetary rings from CelestialBodyLayer
    /// 3. Scatter: sparse random placement in open space
    /// </summary>
    public class AsteroidChunkGenerator : IChunkDataGenerator
    {
        private readonly AsteroidGenerationConfig _config;
        private SpaceZoneQuery _zoneQuery;
        private CelestialBodyQuery _celestialQuery;
        private float _cachedWorldSeed;
        private const int GENERATOR_INDEX = 15;

        public int Priority => 15;
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(AsteroidChunkData);

        public AsteroidChunkGenerator(AsteroidGenerationConfig config)
        {
            _config = config;
        }

        public void Generate(Chunk.Chunk chunk, ChunkGenerationContext context)
        {
            var data = new AsteroidChunkData();

            EnsureQueries(context.WorldSeed);

            var rng = context.GetGeneratorRandom(GENERATOR_INDEX);
            Vector2D chunkCenter = chunk.Coord.ToAbsoluteCenter(context.ChunkSize);
            Vector2 chunkCenterV2 = chunk.GetAbsoluteCenter();

            // 1. Belt asteroids from SpaceZoneLayer
            GenerateBeltAsteroids(chunk, context, rng, chunkCenter, chunkCenterV2, data);

            // 2. Ring asteroids from CelestialBodyLayer
            GenerateRingAsteroids(chunk, context, rng, chunkCenter, chunkCenterV2, data);

            // 3. Scatter asteroids
            GenerateScatterAsteroids(chunk, context, rng, chunkCenterV2, data);

            chunk.SetData(data);
        }

        public void OnChunkUnloading(Chunk.Chunk chunk) { }

        private void GenerateBeltAsteroids(Chunk.Chunk chunk, ChunkGenerationContext context,
            System.Random rng, Vector2D chunkCenter, Vector2 chunkCenterV2, AsteroidChunkData data)
        {
            if (_zoneQuery == null) return;

            Rect chunkBounds = chunk.AbsoluteBounds;
            float cellSize = _config.beltMinSpacing;
            float worldSeed = context.WorldSeed;

            // Seed-based noise offset
            float seedOffsetX = (worldSeed * 137.531f) % 10000f;
            float seedOffsetY = (worldSeed * 251.973f) % 10000f;

            // Iterate world-space cells overlapping this chunk
            int minCellX = Mathf.FloorToInt(chunkBounds.xMin / cellSize);
            int maxCellX = Mathf.FloorToInt(chunkBounds.xMax / cellSize);
            int minCellY = Mathf.FloorToInt(chunkBounds.yMin / cellSize);
            int maxCellY = Mathf.FloorToInt(chunkBounds.yMax / cellSize);

            for (int cx = minCellX; cx <= maxCellX; cx++)
            {
                for (int cy = minCellY; cy <= maxCellY; cy++)
                {
                    // Deterministic hash for this cell
                    uint cellHash = HashCell(cx, cy, worldSeed);

                    // Position within cell — hex offset on odd rows to break grid alignment
                    float rowOffset = (cy & 1) == 0 ? 0f : 0.5f;
                    float px = (cx + rowOffset + HashToFloat(cellHash ^ 0xA1B2C3D4u)) * cellSize;
                    float py = (cy + HashToFloat(cellHash ^ 0xD4C3B2A1u)) * cellSize;

                    // Must be within chunk bounds
                    if (!chunkBounds.Contains(new Vector2(px, py)))
                        continue;

                    // Noise-based displacement for organic clustering
                    float nx = px * _config.jitterNoiseScale + seedOffsetX;
                    float ny = py * _config.jitterNoiseScale + seedOffsetY;
                    px += NoiseUtility.FBM(nx, ny, 3, 0.5f) * _config.jitterAmount;
                    py += NoiseUtility.FBM(nx + 173.7f, ny + 291.3f, 3, 0.5f) * _config.jitterAmount;

                    // Re-check bounds after jitter
                    if (!chunkBounds.Contains(new Vector2(px, py)))
                        continue;

                    // Query fabric density at this position
                    var sample = _zoneQuery.QueryAt(new Vector2D(px, py), worldSeed);
                    float density = sample.FabricSample.AsteroidDensity;
                    if (density < _config.beltMinDensityThreshold)
                        continue;

                    // Density-proportional survival
                    float survivalChance = density * _config.beltDensityMultiplier;

                    // Add noise variation to survival for organic sparse/dense pockets
                    float dnx = px * _config.densityNoiseScale + seedOffsetX;
                    float dny = py * _config.densityNoiseScale + seedOffsetY;
                    float densityNoise = NoiseUtility.Perlin2DNormalized(dnx, dny);
                    survivalChance *= Mathf.Lerp(1f, densityNoise, _config.densityNoiseInfluence);

                    if (HashToFloat(cellHash ^ 0x55AA55AAu) > survivalChance)
                        continue;

                    Vector2 localPos = new Vector2(px, py) - chunkCenterV2;

                    // Size from noise + hash — larger rocks cluster together
                    float sizeNoise = NoiseUtility.Perlin2DNormalized(dnx * 1.7f + 300f, dny * 1.7f + 300f);
                    float sizeT = sizeNoise * 0.6f + HashToFloat(cellHash ^ 0x33u) * 0.4f;
                    float size = Mathf.Lerp(_config.beltSizeRange.x, _config.beltSizeRange.y, sizeT);

                    data.Asteroids.Add(new AsteroidDefinition
                    {
                        LocalPosition = localPos,
                        Size = size,
                        Rotation = HashToFloat(cellHash ^ 0x44u) * 360f,
                        Variant = (int)(HashToFloat(cellHash ^ 0x66u) * _config.variantCount) % _config.variantCount,
                        Seed = HashToFloat(cellHash ^ 0x77u) * 10000f,
                        Source = AsteroidSource.Belt,
                    });
                }
            }
        }

        private static uint HashCell(int x, int y, float seed)
        {
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791f);
            h = ((h >> 16) ^ h) * 0x45D9F3Bu;
            h = ((h >> 16) ^ h) * 0x45D9F3Bu;
            h = (h >> 16) ^ h;
            return h;
        }

        private static float HashToFloat(uint hash)
        {
            return (hash & 0xFFFFu) / 65536f;
        }

        private void GenerateRingAsteroids(Chunk.Chunk chunk, ChunkGenerationContext context,
            System.Random rng, Vector2D chunkCenter, Vector2 chunkCenterV2, AsteroidChunkData data)
        {
            if (_celestialQuery == null) return;

            // Find ringed planets near this chunk
            float searchRadius = chunk.AbsoluteBounds.width * 0.5f + 50000f; // generous search
            var bodies = _celestialQuery.GetBodiesInRadius(chunkCenter, searchRadius, context.WorldSeed);

            Rect chunkBounds = chunk.AbsoluteBounds;
            float gapThreshold = _config.ringGapThreshold;
            float radialNoiseStrength = _config.ringRadialNoise;

            foreach (var body in bodies)
            {
                if (!body.HasRing) continue;

                Vector2 bodyCenterAbsolute = new Vector2((float)body.AbsolutePosition.X, (float)body.AbsolutePosition.Y);
                float ringWidth = body.RingOuterRadius - body.RingInnerRadius;

                // Generate asteroids in ring segments that overlap this chunk
                float angleStep = 360f / _config.ringSegments;
                uint bodySeed = CelestialBodyLayer.Hash2D(
                    Mathf.FloorToInt(bodyCenterAbsolute.x),
                    Mathf.FloorToInt(bodyCenterAbsolute.y),
                    context.WorldSeed);

                for (int seg = 0; seg < _config.ringSegments; seg++)
                {
                    float baseAngle = seg * angleStep;
                    uint segHash = CelestialBodyLayer.HashCombine(bodySeed, (uint)seg);

                    // Per-segment density variation — creates natural gaps
                    float segmentDensity = CelestialBodyLayer.HashToFloat(segHash ^ 0x77u);
                    if (segmentDensity < gapThreshold)
                        continue;

                    for (int a = 0; a < _config.ringAsteroidsPerSegment; a++)
                    {
                        uint aHash = CelestialBodyLayer.HashCombine(segHash, (uint)a);

                        float angle = baseAngle + CelestialBodyLayer.HashToFloat(aHash ^ 0x11u) * angleStep;
                        float radiusT = CelestialBodyLayer.HashToFloat(aHash ^ 0x22u);
                        float orbitRadius = Mathf.Lerp(body.RingInnerRadius, body.RingOuterRadius, radiusT);

                        // Noise-based radial and angular perturbation for organic look
                        float angleRad = angle * Mathf.Deg2Rad;
                        float radialNoise = NoiseUtility.FBM(angle * 0.1f, bodySeed * 0.001f, 2, 0.5f);
                        orbitRadius += radialNoise * ringWidth * radialNoiseStrength;
                        orbitRadius = Mathf.Clamp(orbitRadius, body.RingInnerRadius, body.RingOuterRadius);

                        float angularNoise = NoiseUtility.FBM(angle * 0.1f + 100f, bodySeed * 0.001f + 100f, 2, 0.5f);
                        angleRad += angularNoise * angleStep * Mathf.Deg2Rad * 0.5f;

                        Vector2 asteroidAbsPos = bodyCenterAbsolute + new Vector2(
                            Mathf.Cos(angleRad) * orbitRadius,
                            Mathf.Sin(angleRad) * orbitRadius);

                        // Check if asteroid is within chunk bounds
                        if (!chunkBounds.Contains(asteroidAbsPos))
                            continue;

                        Vector2 localPos = asteroidAbsPos - chunkCenterV2;
                        float size = Mathf.Lerp(_config.ringSizeRange.x, _config.ringSizeRange.y, CelestialBodyLayer.HashToFloat(aHash ^ 0x33u));

                        data.Asteroids.Add(new AsteroidDefinition
                        {
                            LocalPosition = localPos,
                            Size = size,
                            Rotation = CelestialBodyLayer.HashToFloat(aHash ^ 0x44u) * 360f,
                            Variant = (int)(CelestialBodyLayer.HashToFloat(aHash ^ 0x55u) * _config.variantCount) % _config.variantCount,
                            Seed = CelestialBodyLayer.HashToFloat(aHash ^ 0x66u) * 10000f,
                            Source = AsteroidSource.PlanetaryRing,
                        });
                    }
                }
            }
        }

        private void GenerateScatterAsteroids(Chunk.Chunk chunk, ChunkGenerationContext context,
            System.Random rng, Vector2 chunkCenterV2, AsteroidChunkData data)
        {
            if (_zoneQuery == null) return;

            Rect chunkBounds = chunk.AbsoluteBounds;
            float cellSize = _config.beltMinSpacing * 3f;
            float worldSeed = context.WorldSeed;

            int minCellX = Mathf.FloorToInt(chunkBounds.xMin / cellSize);
            int maxCellX = Mathf.FloorToInt(chunkBounds.xMax / cellSize);
            int minCellY = Mathf.FloorToInt(chunkBounds.yMin / cellSize);
            int maxCellY = Mathf.FloorToInt(chunkBounds.yMax / cellSize);

            for (int cx = minCellX; cx <= maxCellX; cx++)
            {
                for (int cy = minCellY; cy <= maxCellY; cy++)
                {
                    // Use a different salt from belt asteroids to avoid overlap
                    uint cellHash = HashCell(cx, cy, worldSeed + 7919f);

                    float px = (cx + HashToFloat(cellHash ^ 0xBB11CC22u)) * cellSize;
                    float py = (cy + HashToFloat(cellHash ^ 0x22CC11BBu)) * cellSize;

                    if (!chunkBounds.Contains(new Vector2(px, py)))
                        continue;

                    // Query fabric density — scatter only in fringe zones (low but nonzero density)
                    var sample = _zoneQuery.QueryAt(new Vector2D(px, py), worldSeed);
                    float density = sample.FabricSample.AsteroidDensity;

                    if (density >= _config.beltMinDensityThreshold || density <= 0.01f)
                        continue;

                    float survivalChance = _config.scatterChance * (density / _config.beltMinDensityThreshold);
                    if (HashToFloat(cellHash ^ 0xEE44FF88u) > survivalChance)
                        continue;

                    Vector2 localPos = new Vector2(px, py) - chunkCenterV2;
                    float size = Mathf.Lerp(_config.scatterSizeRange.x, _config.scatterSizeRange.y, HashToFloat(cellHash ^ 0x99u));

                    data.Asteroids.Add(new AsteroidDefinition
                    {
                        LocalPosition = localPos,
                        Size = size,
                        Rotation = HashToFloat(cellHash ^ 0xAAu) * 360f,
                        Variant = (int)(HashToFloat(cellHash ^ 0xBBu) * _config.variantCount) % _config.variantCount,
                        Seed = HashToFloat(cellHash ^ 0xCCu) * 10000f,
                        Source = AsteroidSource.Scatter,
                    });
                }
            }
        }

        private void EnsureQueries(float worldSeed)
        {
            if (_zoneQuery != null && _celestialQuery != null && Mathf.Approximately(_cachedWorldSeed, worldSeed))
                return;

            var fabricService = WorldFabricService.Instance;
            if (fabricService == null)
            {
                Debug.LogWarning("[AsteroidChunkGenerator] WorldFabricService.Instance is null — asteroid zone queries unavailable.");
                _zoneQuery = null;
                _celestialQuery = null;
                return;
            }

            fabricService.EnsureEditModeQueries();
            _zoneQuery = fabricService.GetSpaceZoneQuery();
            _celestialQuery = fabricService.GetCelestialBodyQuery();
            _cachedWorldSeed = worldSeed;

            if (_zoneQuery == null)
                Debug.LogWarning("[AsteroidChunkGenerator] SpaceZoneQuery is null — belt and scatter asteroids will not generate.");
            if (_celestialQuery == null)
                Debug.LogWarning("[AsteroidChunkGenerator] CelestialBodyQuery is null — ring asteroids will not generate.");
        }
    }
}
