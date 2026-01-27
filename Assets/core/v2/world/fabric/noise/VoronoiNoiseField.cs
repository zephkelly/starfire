using System;
using UnityEngine;
using Starfire.Core.V2.World;

namespace StarfireV2
{
    /// <summary>
    /// Voronoi/cellular noise for territory boundaries and region-based generation.
    /// Supports optional domain warping for organic borders, faceted (low-poly) border mode,
    /// and resource-biased distance for border attraction.
    /// </summary>
    [Serializable]
    public class VoronoiNoiseField : INoiseField
    {
        [SerializeField] private float cellSize = 5000f;
        [Range(0f, 1f)]
        [SerializeField] private float jitter = 0.8f;

        // Domain warping for organic borders
        private readonly PerlinNoiseField _warpNoiseX;
        private readonly PerlinNoiseField _warpNoiseY;
        private readonly float _warpStrength;

        // Edge noise for fine border detail (disabled in faceted mode)
        private readonly PerlinNoiseField _edgeNoise;
        private readonly float _edgeNoiseStrength;

        // Faceted border mode: quantizes warp to create low-poly angular borders
        private readonly bool _useFacetedWarp;
        private readonly int _facetAngularSteps;

        public float CellSize { get => cellSize; set => cellSize = Mathf.Max(100f, value); }
        public float Jitter { get => jitter; set => jitter = Mathf.Clamp01(value); }

        public VoronoiNoiseField() { }

        public VoronoiNoiseField(float cellSize, float jitter = 0.8f)
        {
            this.cellSize = cellSize;
            this.jitter = jitter;
            _warpStrength = 0f;
        }

        public VoronoiNoiseField(float cellSize, float jitter, float warpStrength, float warpScale, int warpOctaves)
        {
            this.cellSize = cellSize;
            this.jitter = jitter;
            _warpStrength = warpStrength;

            if (warpStrength > 0f)
            {
                _warpNoiseX = new PerlinNoiseField(warpScale, warpOctaves, 0.5f);
                _warpNoiseY = new PerlinNoiseField(warpScale, warpOctaves, 0.5f);
            }
        }

        public VoronoiNoiseField(float cellSize, float jitter, float warpStrength, float warpScale, int warpOctaves,
            float edgeNoiseStrength, float edgeNoiseScale)
        {
            this.cellSize = cellSize;
            this.jitter = jitter;
            _warpStrength = warpStrength;

            if (warpStrength > 0f)
            {
                _warpNoiseX = new PerlinNoiseField(warpScale, warpOctaves, 0.5f);
                _warpNoiseY = new PerlinNoiseField(warpScale, warpOctaves, 0.5f);
            }

            if (edgeNoiseStrength > 0f)
            {
                _edgeNoiseStrength = edgeNoiseStrength;
                _edgeNoise = new PerlinNoiseField(edgeNoiseScale, 3, 0.5f);
            }
        }

        /// <summary>
        /// Constructor with faceted border support.
        /// When faceted is true, domain warp is quantized to discrete angular steps
        /// producing low-poly straight-segment borders, and edge noise is disabled.
        /// </summary>
        public VoronoiNoiseField(float cellSize, float jitter, float warpStrength, float warpScale, int warpOctaves,
            float edgeNoiseStrength, float edgeNoiseScale,
            bool useFacetedWarp, int facetAngularSteps)
        {
            this.cellSize = cellSize;
            this.jitter = jitter;
            _warpStrength = warpStrength;
            _useFacetedWarp = useFacetedWarp;
            _facetAngularSteps = Mathf.Max(4, facetAngularSteps);

            if (warpStrength > 0f)
            {
                _warpNoiseX = new PerlinNoiseField(warpScale, warpOctaves, 0.5f);
                _warpNoiseY = new PerlinNoiseField(warpScale, warpOctaves, 0.5f);
            }

            // Edge noise is incompatible with faceted mode (would smooth out the facets)
            if (!useFacetedWarp && edgeNoiseStrength > 0f)
            {
                _edgeNoiseStrength = edgeNoiseStrength;
                _edgeNoise = new PerlinNoiseField(edgeNoiseScale, 3, 0.5f);
            }
        }

        public float Sample(Vector2D position)
        {
            var result = SampleDetailed(position, 0f);
            return result.NormalizedDistanceToEdge;
        }

        public float Sample(Vector2D position, float seed)
        {
            var result = SampleDetailed(position, seed);
            return result.NormalizedDistanceToEdge;
        }

        public VoronoiResult SampleDetailed(Vector2D position, float seed = 0f)
        {
            return SampleDetailedInternal(position, seed, 0f);
        }

        /// <summary>
        /// Sample with resource bias. Positive resourceBias makes the nearest cell
        /// "win" more strongly, causing borders to bulge toward resource-rich areas.
        /// resourceBias is pre-scaled (e.g., overallResourceValue * attractionStrength).
        /// </summary>
        public VoronoiResult SampleDetailed(Vector2D position, float seed, float resourceBias)
        {
            return SampleDetailedInternal(position, seed, resourceBias);
        }

        private VoronoiResult SampleDetailedInternal(Vector2D position, float seed, float resourceBias)
        {
            Vector2D samplePos = ApplyWarp(position, seed);

            double scaledX = samplePos.X / cellSize;
            double scaledY = samplePos.Y / cellSize;

            int cellX = (int)Math.Floor(scaledX);
            int cellY = (int)Math.Floor(scaledY);

            float minDist = float.MaxValue;
            float secondMinDist = float.MaxValue;
            int nearestCellId = 0;
            Vector2D nearestCenter = Vector2D.Zero;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = cellX + dx;
                    int ny = cellY + dy;

                    Vector2D cellCenter = GetCellCenter(nx, ny, seed);

                    double distX = samplePos.X - cellCenter.X;
                    double distY = samplePos.Y - cellCenter.Y;
                    float dist = (float)Math.Sqrt(distX * distX + distY * distY);

                    if (dist < minDist)
                    {
                        secondMinDist = minDist;
                        minDist = dist;
                        nearestCellId = HashCellCoord(nx, ny, seed);
                        nearestCenter = cellCenter;
                    }
                    else if (dist < secondMinDist)
                    {
                        secondMinDist = dist;
                    }
                }
            }

            // Resource attraction: reduce nearest distance so the nearest cell
            // expands into resource-rich areas, pulling the border toward resources
            if (resourceBias > 0f)
            {
                minDist -= resourceBias * cellSize;
            }

            float distanceToEdge = (secondMinDist - minDist) * 0.5f;

            // Apply edge noise for fine border detail (only near borders, disabled in faceted mode)
            if (_edgeNoise != null && !_useFacetedWarp && distanceToEdge < cellSize * 0.3f)
            {
                float edgePerturbation = (_edgeNoise.Sample(position, seed) - 0.5f) * 2f * _edgeNoiseStrength;
                distanceToEdge += edgePerturbation;
                distanceToEdge = Mathf.Max(0f, distanceToEdge);
            }

            return new VoronoiResult
            {
                CellId = nearestCellId,
                DistanceToCenter = minDist,
                DistanceToEdge = distanceToEdge,
                NormalizedDistanceToEdge = Mathf.Clamp01(distanceToEdge / (cellSize * 0.5f)),
                CellCenter = nearestCenter
            };
        }

        public int GetCellIdAt(Vector2D position, float seed = 0f)
        {
            Vector2D samplePos = ApplyWarp(position, seed);

            double scaledX = samplePos.X / cellSize;
            double scaledY = samplePos.Y / cellSize;

            int cellX = (int)Math.Floor(scaledX);
            int cellY = (int)Math.Floor(scaledY);

            float minDist = float.MaxValue;
            int nearestCellId = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = cellX + dx;
                    int ny = cellY + dy;

                    Vector2D cellCenter = GetCellCenter(nx, ny, seed);

                    double distX = samplePos.X - cellCenter.X;
                    double distY = samplePos.Y - cellCenter.Y;
                    float dist = (float)(distX * distX + distY * distY);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestCellId = HashCellCoord(nx, ny, seed);
                    }
                }
            }

            return nearestCellId;
        }

        /// <summary>
        /// Apply domain warping. In faceted mode, warp direction and magnitude are
        /// quantized to produce piecewise-linear (low-poly) border segments.
        /// </summary>
        private Vector2D ApplyWarp(Vector2D position, float seed)
        {
            if (_warpStrength <= 0f || _warpNoiseX == null)
                return position;

            float rawWarpX = (_warpNoiseX.Sample(position, seed) - 0.5f) * 2f;
            float rawWarpY = (_warpNoiseY.Sample(position, seed + 1000f) - 0.5f) * 2f;

            float warpX, warpY;

            if (_useFacetedWarp)
            {
                // Convert to polar, quantize angle and magnitude for faceted borders
                float angle = Mathf.Atan2(rawWarpY, rawWarpX);
                float magnitude = Mathf.Sqrt(rawWarpX * rawWarpX + rawWarpY * rawWarpY);

                // Quantize angle to discrete steps → straight border segments
                float stepSize = (2f * Mathf.PI) / _facetAngularSteps;
                angle = Mathf.Round(angle / stepSize) * stepSize;

                // Quantize magnitude to discrete levels → uniform segment lengths
                magnitude = Mathf.Round(magnitude * 3f) / 3f;

                warpX = Mathf.Cos(angle) * magnitude * _warpStrength;
                warpY = Mathf.Sin(angle) * magnitude * _warpStrength;
            }
            else
            {
                warpX = rawWarpX * _warpStrength;
                warpY = rawWarpY * _warpStrength;
            }

            return new Vector2D(position.X + warpX, position.Y + warpY);
        }

        private Vector2D GetCellCenter(int cellX, int cellY, float seed)
        {
            int seedInt = (int)(seed * 1000);

            // Use two independent hashes for X and Y jitter to avoid correlation
            float jitterX = (HashToFloat(MixHash(cellX, cellY, seedInt)) - 0.5f) * jitter;
            float jitterY = (HashToFloat(MixHash(cellX, cellY, seedInt + 7919)) - 0.5f) * jitter;

            return new Vector2D(
                (cellX + 0.5f + jitterX) * cellSize,
                (cellY + 0.5f + jitterY) * cellSize
            );
        }

        private static int HashCellCoord(int x, int y, float seed)
        {
            return MixHash(x, y, (int)(seed * 1000));
        }

        /// <summary>
        /// Avalanche-quality integer hash combining cell coordinates and seed.
        /// Ensures adjacent cells produce uncorrelated hash values.
        /// </summary>
        private static int MixHash(int x, int y, int seed)
        {
            // Combine inputs with large primes to break linearity
            uint h = (uint)x * 0x9E3779B1u;  // golden ratio derived
            h ^= (uint)y * 0x517CC1B7u;       // another large prime
            h ^= (uint)seed * 0x6C62272Eu;

            // Avalanche mixing (MurmurHash3 finalizer)
            h ^= h >> 16;
            h *= 0x85EBCA6Bu;
            h ^= h >> 13;
            h *= 0xC2B2AE35u;
            h ^= h >> 16;

            return (int)(h & 0x7FFFFFFFu);
        }

        private static float HashToFloat(int hash)
        {
            return (hash & 0xFFFFFF) / 16777216f;
        }
    }

    public struct VoronoiResult
    {
        public int CellId;
        public float DistanceToCenter;
        public float DistanceToEdge;
        public float NormalizedDistanceToEdge;
        public Vector2D CellCenter;
    }
}
