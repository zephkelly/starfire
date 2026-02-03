using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace StarfireV2
{
    /// <summary>
    /// World fabric layer that deterministically places stars and planets using spatial hashing.
    /// Stars are placed in a grid with jitter; planets orbit their parent star at deterministic radii/angles.
    /// </summary>
    public class CelestialBodyLayer : IWorldLayer
    {
        public string LayerId => "CelestialBodies";
        public int Priority => 2;
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(CelestialBodyLayerData);
        public IReadOnlyList<Type> Dependencies => Array.Empty<Type>();

        private readonly CelestialBodyConfig _config;

        public CelestialBodyLayer(CelestialBodyConfig config)
        {
            _config = config;
        }

        public WorldLayerData Generate(Chunk chunk, WorldFabricContext context)
        {
            var data = new CelestialBodyLayerData();
            Vector2D chunkCenter = chunk.Coord.ToAbsoluteCenter(context.ChunkSize);
            float searchRadius = _config.starSpacing + GetMaxPlanetOrbitRadius();

            CollectBodiesInRadius(chunkCenter, searchRadius, context.WorldSeed, data.Bodies);
            return data;
        }

        public IWorldLayerQuery CreateQuery() => new CelestialBodyQuery(_config, this);

        public void OnChunkUnloading(Chunk chunk) { }

        /// <summary>
        /// Collect all celestial bodies (stars + their planets) within radius of a point.
        /// </summary>
        internal void CollectBodiesInRadius(Vector2D center, float radius, float worldSeed, List<CelestialBodyInfo> results)
        {
            float cellSize = _config.starSpacing;
            int searchCells = Mathf.CeilToInt(radius / cellSize) + 1;

            int centerCellX = Mathf.FloorToInt((float)center.X / cellSize);
            int centerCellY = Mathf.FloorToInt((float)center.Y / cellSize);

            float radiusSq = radius * radius;

            for (int dx = -searchCells; dx <= searchCells; dx++)
            {
                for (int dy = -searchCells; dy <= searchCells; dy++)
                {
                    int cx = centerCellX + dx;
                    int cy = centerCellY + dy;

                    uint cellHash = Hash2D(cx, cy, worldSeed);

                    // Star spawn check
                    float spawnRoll = HashToFloat(cellHash);
                    if (spawnRoll > _config.starSpawnChance) continue;

                    // Star position (jittered within cell)
                    float starX = (cx + 0.1f + HashToFloat(cellHash ^ 0xA5A5A5A5u) * 0.8f) * cellSize;
                    float starY = (cy + 0.1f + HashToFloat(cellHash ^ 0x5A5A5A5Au) * 0.8f) * cellSize;
                    Vector2D starPos = new Vector2D(starX, starY);

                    // Select star type
                    float typeRoll = HashToFloat(cellHash ^ 0x12345678u);
                    var starTypeConfig = _config.SelectStarType(typeRoll);
                    if (starTypeConfig == null) continue;

                    float radiusT = HashToFloat(cellHash ^ 0xDEADBEEFu);
                    float starRadius = Mathf.Lerp(starTypeConfig.radiusRange.x, starTypeConfig.radiusRange.y, radiusT);
                    float starMass = Mathf.Lerp(starTypeConfig.massRange.x, starTypeConfig.massRange.y, radiusT);
                    float starGravityRadius = starRadius * _config.gravityRadiusMultiplier;
                    float starSeed = HashToFloat(cellHash ^ 0xCAFEBABEu);

                    var star = new CelestialBodyInfo
                    {
                        Kind = CelestialBodyKind.Star,
                        AbsolutePosition = starPos,
                        Radius = starRadius,
                        Mass = starMass,
                        GravityRadius = starGravityRadius,
                        Seed = starSeed,
                        StarType = starTypeConfig.type,
                        Luminosity = starTypeConfig.luminosity,
                    };

                    // Check if star or any of its planets could be within search radius
                    float maxSystemRadius = starGravityRadius + GetMaxPlanetOrbitRadius();
                    double distSq = (starPos - center).SqrMagnitude;
                    if (distSq > (radius + maxSystemRadius) * (radius + maxSystemRadius)) continue;

                    // Add star if close enough
                    if (distSq <= radiusSq + starGravityRadius * starGravityRadius)
                    {
                        results.Add(star);
                    }

                    // Generate planets for this star
                    GeneratePlanets(cellHash, starPos, starMass, worldSeed, center, radius, results);
                }
            }
        }

        private void GeneratePlanets(uint starHash, Vector2D starPos, float starMass, float worldSeed,
            Vector2D searchCenter, float searchRadius, List<CelestialBodyInfo> results)
        {
            // Determine planet count from star hash
            uint planetSeed = starHash ^ 0x77777777u;
            int minPlanets = _config.planetsPerStarRange.x;
            int maxPlanets = _config.planetsPerStarRange.y;
            int planetCount = minPlanets + Mathf.FloorToInt(HashToFloat(planetSeed) * (maxPlanets - minPlanets + 1));
            planetCount = Mathf.Min(planetCount, maxPlanets);

            float searchRadiusSq = searchRadius * searchRadius;

            for (int i = 0; i < planetCount; i++)
            {
                uint pHash = HashCombine(planetSeed, (uint)i);

                // Orbit radius: distribute planets with increasing orbits
                float orbitT = (i + 0.5f) / planetCount;
                float orbitJitter = (HashToFloat(pHash ^ 0x11111111u) - 0.5f) * 0.3f;
                orbitT = Mathf.Clamp01(orbitT + orbitJitter);
                float orbitRadius = Mathf.Lerp(_config.planetOrbitRadiusRange.x, _config.planetOrbitRadiusRange.y, orbitT);

                // Orbit angle
                float orbitAngle = HashToFloat(pHash ^ 0x22222222u) * 360f;

                // Planet position
                float rad = orbitAngle * Mathf.Deg2Rad;
                Vector2D planetPos = starPos + new Vector2D(
                    Mathf.Cos(rad) * orbitRadius,
                    Mathf.Sin(rad) * orbitRadius
                );

                // Early distance check
                double distSq = (planetPos - searchCenter).SqrMagnitude;
                float maxPlanetInfluence = _config.planetOrbitRadiusRange.y * 0.1f; // rough gravity estimate
                if (distSq > (searchRadius + maxPlanetInfluence) * (searchRadius + maxPlanetInfluence)) continue;

                // Select planet type
                float typeRoll = HashToFloat(pHash ^ 0x33333333u);
                var planetTypeConfig = _config.SelectPlanetType(typeRoll);
                if (planetTypeConfig == null) continue;

                float radiusT = HashToFloat(pHash ^ 0x44444444u);
                float planetRadius = Mathf.Lerp(planetTypeConfig.radiusRange.x, planetTypeConfig.radiusRange.y, radiusT);
                float planetMass = Mathf.Lerp(planetTypeConfig.massRange.x, planetTypeConfig.massRange.y, radiusT);
                float planetGravityRadius = planetRadius * _config.gravityRadiusMultiplier;
                float parallaxT = HashToFloat(pHash ^ 0x55555555u);
                float parallaxDepth = Mathf.Lerp(planetTypeConfig.parallaxRange.x, planetTypeConfig.parallaxRange.y, parallaxT);

                // Ring check
                bool hasRing = HashToFloat(pHash ^ 0x66666666u) < planetTypeConfig.ringChance;
                float ringInner = hasRing ? planetRadius * _config.ringInnerRadiusMult : 0f;
                float ringOuter = hasRing ? planetRadius * _config.ringOuterRadiusMult : 0f;

                var planet = new CelestialBodyInfo
                {
                    Kind = CelestialBodyKind.Planet,
                    AbsolutePosition = planetPos,
                    Radius = planetRadius,
                    Mass = planetMass,
                    GravityRadius = planetGravityRadius,
                    Seed = HashToFloat(pHash ^ 0x77777777u),
                    PlanetType = planetTypeConfig.type,
                    ParentStarPosition = starPos,
                    OrbitRadius = orbitRadius,
                    OrbitAngle = orbitAngle,
                    ParallaxDepthFactor = parallaxDepth,
                    HasRing = hasRing,
                    RingInnerRadius = ringInner,
                    RingOuterRadius = ringOuter,
                };

                results.Add(planet);
            }
        }

        private float GetMaxPlanetOrbitRadius()
        {
            return _config.planetOrbitRadiusRange.y;
        }

        // --- Hash utilities (same as MetaballField for consistency) ---

        internal static uint Hash2D(int x, int y, float seed)
        {
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791f);
            h = ((h >> 16) ^ h) * 0x45D9F3Bu;
            h = ((h >> 16) ^ h) * 0x45D9F3Bu;
            h = (h >> 16) ^ h;
            return h;
        }

        internal static float HashToFloat(uint hash)
        {
            return (hash & 0xFFFFu) / 65536f;
        }

        internal static uint HashCombine(uint a, uint b)
        {
            uint h = a ^ (b * 0x9E3779B9u + (a << 6) + (a >> 2));
            h = ((h >> 16) ^ h) * 0x45D9F3Bu;
            h = ((h >> 16) ^ h) * 0x45D9F3Bu;
            return (h >> 16) ^ h;
        }
    }
}
