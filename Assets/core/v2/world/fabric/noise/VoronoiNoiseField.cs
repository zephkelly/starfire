using System;
using UnityEngine;
using Starfire.Core.V2.World;

namespace StarfireV2
{
    /// <summary>
    /// Voronoi/cellular noise for territory boundaries and region-based generation.
    /// Supports optional domain warping for organic, natural-looking borders.
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

        // Edge noise for fine border detail
        private readonly PerlinNoiseField _edgeNoise;
        private readonly float _edgeNoiseStrength;

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
            // Apply domain warping for organic borders
            Vector2D samplePos = position;
            if (_warpStrength > 0f && _warpNoiseX != null)
            {
                // Perlin returns [0,1], convert to [-1,1] then scale by warp strength
                float warpX = (_warpNoiseX.Sample(position, seed) - 0.5f) * 2f * _warpStrength;
                float warpY = (_warpNoiseY.Sample(position, seed + 1000f) - 0.5f) * 2f * _warpStrength;
                samplePos = new Vector2D(position.X + warpX, position.Y + warpY);
            }

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

                    // Use warped position for distance calculation to create organic borders
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

            float distanceToEdge = (secondMinDist - minDist) * 0.5f;

            // Apply edge noise for fine border detail (only near borders)
            if (_edgeNoise != null && distanceToEdge < cellSize * 0.3f)
            {
                float edgePerturbation = (_edgeNoise.Sample(position, seed) - 0.5f) * 2f * _edgeNoiseStrength;
                distanceToEdge += edgePerturbation;
                distanceToEdge = Mathf.Max(0f, distanceToEdge); // Prevent negative distances
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
            // Apply domain warping for organic borders
            Vector2D samplePos = position;
            if (_warpStrength > 0f && _warpNoiseX != null)
            {
                float warpX = (_warpNoiseX.Sample(position, seed) - 0.5f) * 2f * _warpStrength;
                float warpY = (_warpNoiseY.Sample(position, seed + 1000f) - 0.5f) * 2f * _warpStrength;
                samplePos = new Vector2D(position.X + warpX, position.Y + warpY);
            }

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

                    // Use warped position for distance calculation to create organic borders
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

        private Vector2D GetCellCenter(int cellX, int cellY, float seed)
        {
            int hash = HashCellCoord(cellX, cellY, seed);

            float jitterX = (HashToFloat(hash) - 0.5f) * jitter;
            float jitterY = (HashToFloat(hash * 16807) - 0.5f) * jitter;

            return new Vector2D(
                (cellX + 0.5f + jitterX) * cellSize,
                (cellY + 0.5f + jitterY) * cellSize
            );
        }

        private static int HashCellCoord(int x, int y, float seed)
        {
            int hash = 17;
            hash = hash * 31 + x;
            hash = hash * 31 + y;
            hash = hash * 31 + (int)(seed * 1000);
            return hash & 0x7FFFFFFF;
        }

        private static float HashToFloat(int hash)
        {
            return (hash & 0xFFFF) / 65536f;
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
