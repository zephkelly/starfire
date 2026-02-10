using Unity.Collections;
using Unity.Mathematics;

namespace Starfire.Demo
{
    public struct StarSpawnData
    {
        public double2 Position;
        public byte SpectralType;
        public float Luminosity;
        public float Mass;
        public float Radius;
        public float SystemRadius;
        public uint Seed;
        public float GravityRange;
        public float GravityStrength;
        public float RadiationRadius;
    }

    public struct AsteroidSpawnData
    {
        public double2 Position;
        public float Size;
        public byte Composition;
        public int ParentStarId;
        public float2 OrbitalVelocity;
    }

    public static class SolarSystemGenerator
    {
        const float MinStarSpacing = 15000f;
        const int MaxPlacementAttempts = 5;

        public static void Generate(
            uint worldSeed,
            float worldRadius,
            int targetStarCount,
            int targetAsteroidCount,
            out NativeList<StarSpawnData> stars,
            out NativeList<AsteroidSpawnData> asteroids)
        {
            stars = new NativeList<StarSpawnData>(targetStarCount, Allocator.Temp);
            asteroids = new NativeList<AsteroidSpawnData>(targetAsteroidCount, Allocator.Temp);

            var rng = new Random(worldSeed);

            PlaceStars(ref rng, worldRadius, targetStarCount, ref stars);

            int asteroidsPerStar = targetAsteroidCount / math.max(stars.Length, 1);
            int remaining = targetAsteroidCount;

            for (int s = 0; s < stars.Length && remaining > 0; s++)
            {
                int count = s < stars.Length - 1 ? asteroidsPerStar : remaining;
                count = math.min(count, remaining);

                GenerateAsteroidBelts(ref rng, stars[s], s, count, ref asteroids);
                remaining -= count;
            }
        }

        static void PlaceStars(ref Random rng, float worldRadius, int targetCount, ref NativeList<StarSpawnData> stars)
        {
            float cellSize = worldRadius * 2f / math.max((int)math.ceil(math.sqrt(targetCount * 1.5f)), 1);
            int gridSize = (int)math.ceil(worldRadius * 2f / cellSize);
            float halfWorld = worldRadius;

            var occupiedCells = new NativeHashSet<long>(targetCount * 2, Allocator.Temp);

            for (int attempt = 0; attempt < targetCount * MaxPlacementAttempts && stars.Length < targetCount; attempt++)
            {
                float x = rng.NextFloat(-halfWorld, halfWorld);
                float y = rng.NextFloat(-halfWorld, halfWorld);

                float distFromCenter = math.sqrt(x * x + y * y);
                if (distFromCenter > worldRadius * 0.95f)
                    continue;

                float densityChance = 1f - (distFromCenter / worldRadius) * 0.5f;
                float clusterNoise = noise.snoise(new float2(x * 0.00005f, y * 0.00005f));
                densityChance *= math.saturate(0.5f + clusterNoise * 0.6f);

                if (rng.NextFloat() > densityChance)
                    continue;

                int cellX = (int)math.floor((x + halfWorld) / cellSize);
                int cellY = (int)math.floor((y + halfWorld) / cellSize);
                long cellKey = (long)cellX * 100003L + cellY;

                if (occupiedCells.Contains(cellKey))
                    continue;

                bool tooClose = false;
                for (int i = 0; i < stars.Length && !tooClose; i++)
                {
                    double2 d = new double2(x, y) - stars[i].Position;
                    double dSq = d.x * d.x + d.y * d.y;
                    if (dSq < (double)MinStarSpacing * MinStarSpacing)
                        tooClose = true;
                }
                if (tooClose) continue;

                occupiedCells.Add(cellKey);

                uint starSeed = (uint)(cellX * 73856093 + cellY * 19349663 + stars.Length * 83492791);
                var starRng = new Random(starSeed | 1);

                byte spectralType = PickSpectralType(ref starRng);
                float mass = SpectralMass(spectralType, ref starRng);
                float luminosity = mass * mass * mass;
                float radius = mass * 50f;
                float systemRadius = mass * 8000f + starRng.NextFloat(2000f, 5000f);

                stars.Add(new StarSpawnData
                {
                    Position = new double2(x, y),
                    SpectralType = spectralType,
                    Luminosity = luminosity,
                    Mass = mass,
                    Radius = radius,
                    SystemRadius = systemRadius,
                    Seed = starSeed,
                    GravityRange = systemRadius * 0.8f,
                    GravityStrength = mass * 100_000_000f,
                    RadiationRadius = radius * 3f
                });
            }

            occupiedCells.Dispose();
        }

        static byte PickSpectralType(ref Random rng)
        {
            float roll = rng.NextFloat();
            if (roll < 0.76f) return 6;
            if (roll < 0.88f) return 5;
            if (roll < 0.95f) return 4;
            if (roll < 0.97f) return 3;
            if (roll < 0.99f) return 2;
            if (roll < 0.998f) return 1;
            return 0;
        }

        static float SpectralMass(byte spectralType, ref Random rng)
        {
            return spectralType switch
            {
                0 => rng.NextFloat(16f, 50f),
                1 => rng.NextFloat(2.1f, 16f),
                2 => rng.NextFloat(1.4f, 2.1f),
                3 => rng.NextFloat(1.04f, 1.4f),
                4 => rng.NextFloat(0.8f, 1.04f),
                5 => rng.NextFloat(0.45f, 0.8f),
                _ => rng.NextFloat(0.08f, 0.45f),
            };
        }

        static void GenerateAsteroidBelts(ref Random rng, StarSpawnData star, int starIndex,
            int totalCount, ref NativeList<AsteroidSpawnData> asteroids)
        {
            var beltRng = new Random(star.Seed ^ 0xDEADBEEF);
            int beltCount = beltRng.NextInt(1, 4);

            float minBeltRadius = star.RadiationRadius + 500f;
            float maxBeltRadius = star.SystemRadius * 0.9f;
            float beltSpan = (maxBeltRadius - minBeltRadius) / beltCount;

            int remaining = totalCount;

            for (int b = 0; b < beltCount && remaining > 0; b++)
            {
                float beltInner = minBeltRadius + beltSpan * b + beltRng.NextFloat(0f, beltSpan * 0.1f);
                float beltOuter = beltInner + beltSpan * beltRng.NextFloat(0.3f, 0.8f);
                byte beltComposition = (byte)beltRng.NextInt(0, 4);
                bool clockwise = beltRng.NextBool();

                int beltAsteroids = b < beltCount - 1 ? remaining / (beltCount - b) : remaining;
                beltAsteroids = math.min(beltAsteroids, remaining);

                for (int a = 0; a < beltAsteroids; a++)
                {
                    float angle = rng.NextFloat(0f, math.PI * 2f);
                    float dist = rng.NextFloat(beltInner, beltOuter);
                    var pos = star.Position + new double2(math.cos(angle) * dist, math.sin(angle) * dist);

                    float sizeRoll = rng.NextFloat();
                    float size;
                    if (sizeRoll < 0.6f) size = rng.NextFloat(0.2f, 1.0f);
                    else if (sizeRoll < 0.9f) size = rng.NextFloat(1.0f, 2.5f);
                    else size = rng.NextFloat(2.5f, 5.0f);

                    byte composition = rng.NextFloat() < 0.7f
                        ? beltComposition
                        : (byte)rng.NextInt(0, 4);

                    float orbitalSpeed = math.sqrt(star.GravityStrength / dist);
                    float2 radial = new float2(math.cos(angle), math.sin(angle));
                    float2 tangent = new float2(-radial.y, radial.x);
                    if (clockwise) tangent = -tangent;

                    asteroids.Add(new AsteroidSpawnData
                    {
                        Position = pos,
                        Size = size,
                        Composition = composition,
                        ParentStarId = starIndex,
                        OrbitalVelocity = tangent * orbitalSpeed
                    });
                }

                remaining -= beltAsteroids;
            }
        }
    }
}
