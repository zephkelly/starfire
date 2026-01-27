using System;
using UnityEngine;
using Starfire.Core.V2.World;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for a single metaball field layer.
    /// Each field generates scattered implicit blobs that merge smoothly.
    /// </summary>
    [Serializable]
    public class MetaballFieldConfig
    {
        [Tooltip("Minimum distance between blob centers (controls region spacing)")]
        public float blobSpacing = 200000f;

        [Tooltip("Minimum blob radius")]
        public float blobRadiusMin = 80000f;

        [Tooltip("Maximum blob radius")]
        public float blobRadiusMax = 300000f;

        [Tooltip("Contribution strength per blob (higher = denser regions)")]
        [Range(0.1f, 2f)]
        public float blobStrength = 1f;

        [Tooltip("Falloff curve power (higher = sharper edges)")]
        [Range(1f, 5f)]
        public float falloffPower = 2f;

        [Tooltip("Threshold below which the field reads as 'not present'")]
        [Range(0f, 1f)]
        public float threshold = 0.3f;
    }

    /// <summary>
    /// Implicit surface field using scattered blobs (metaballs).
    /// Blobs are deterministically placed using spatial hashing, so any position
    /// can be queried without pre-generating the entire field.
    ///
    /// Unlike Perlin noise which has energy at all frequencies (creating "scars"),
    /// metaballs only have energy where blobs exist. Regions between blobs are
    /// perfectly clean, and overlapping blobs merge into smooth, bubble-like shapes.
    /// </summary>
    public class MetaballField : INoiseField
    {
        private readonly float _blobSpacing;
        private readonly float _blobRadiusMin;
        private readonly float _blobRadiusMax;
        private readonly float _blobStrength;
        private readonly float _falloffPower;
        private readonly float _threshold;

        // Grid cell size determines the spatial hash grid.
        // Must be >= max blob radius so we only check neighboring cells.
        private readonly float _cellSize;

        // How many neighboring cells to check (based on max radius vs cell size)
        private readonly int _searchRadius;

        // Seed offset for this field instance (avoids correlation between fields)
        private readonly float _seedOffset;

        public MetaballField(MetaballFieldConfig config, float seedOffset = 0f)
        {
            _blobSpacing = config.blobSpacing;
            _blobRadiusMin = config.blobRadiusMin;
            _blobRadiusMax = config.blobRadiusMax;
            _blobStrength = config.blobStrength;
            _falloffPower = config.falloffPower;
            _threshold = config.threshold;
            _seedOffset = seedOffset;

            // Cell size should be large enough that a blob in one cell can't reach
            // beyond the search radius. We use blob spacing as cell size since
            // blobs are roughly one per cell.
            _cellSize = Mathf.Max(_blobSpacing, _blobRadiusMax);

            // Search radius: how many cells away a blob could still contribute
            _searchRadius = Mathf.CeilToInt(_blobRadiusMax / _cellSize) + 1;
        }

        public MetaballField(
            float blobSpacing,
            float blobRadiusMin,
            float blobRadiusMax,
            float blobStrength = 1f,
            float falloffPower = 2f,
            float threshold = 0.3f,
            float seedOffset = 0f)
        {
            _blobSpacing = blobSpacing;
            _blobRadiusMin = blobRadiusMin;
            _blobRadiusMax = blobRadiusMax;
            _blobStrength = blobStrength;
            _falloffPower = falloffPower;
            _threshold = threshold;
            _seedOffset = seedOffset;

            _cellSize = Mathf.Max(_blobSpacing, _blobRadiusMax);
            _searchRadius = Mathf.CeilToInt(_blobRadiusMax / _cellSize) + 1;
        }

        /// <summary>
        /// Sample the raw field value at a position (0 = no blob contribution, higher = more).
        /// </summary>
        public float Sample(Vector2D position)
        {
            return SampleInternal((float)position.X, (float)position.Y, 0f);
        }

        /// <summary>
        /// Sample with world seed for deterministic variation.
        /// </summary>
        public float Sample(Vector2D position, float seed)
        {
            return SampleInternal((float)position.X, (float)position.Y, seed + _seedOffset);
        }

        /// <summary>
        /// Sample and return both the raw density and whether it's above threshold.
        /// </summary>
        public MetaballSampleResult SampleDetailed(Vector2D position, float seed)
        {
            float density = SampleInternal((float)position.X, (float)position.Y, seed + _seedOffset);
            return new MetaballSampleResult
            {
                RawDensity = density,
                NormalizedDensity = Mathf.Clamp01(density),
                IsActive = density >= _threshold
            };
        }

        private float SampleInternal(float x, float y, float seed)
        {
            // Determine which grid cell this position falls in
            int cellX = Mathf.FloorToInt(x / _cellSize);
            int cellY = Mathf.FloorToInt(y / _cellSize);

            float totalContribution = 0f;

            // Check nearby cells for blobs that could contribute
            for (int dx = -_searchRadius; dx <= _searchRadius; dx++)
            {
                for (int dy = -_searchRadius; dy <= _searchRadius; dy++)
                {
                    int cx = cellX + dx;
                    int cy = cellY + dy;

                    // Determine how many blobs this cell has and their properties
                    // Use deterministic hashing so the same cell always produces the same blobs
                    uint cellHash = Hash2D(cx, cy, seed);

                    // Probability of a blob existing in this cell
                    // Higher spacing relative to cell size = fewer blobs
                    float spawnRoll = HashToFloat(cellHash);
                    float spawnChance = (_cellSize * _cellSize) / (_blobSpacing * _blobSpacing);
                    spawnChance = Mathf.Clamp01(spawnChance);

                    if (spawnRoll > spawnChance) continue;

                    // Blob center position within the cell (jittered)
                    float blobX = (cx + HashToFloat(cellHash ^ 0xA5A5A5A5u)) * _cellSize;
                    float blobY = (cy + HashToFloat(cellHash ^ 0x5A5A5A5Au)) * _cellSize;

                    // Blob radius (varies per blob)
                    float radiusT = HashToFloat(cellHash ^ 0x12345678u);
                    float blobRadius = Mathf.Lerp(_blobRadiusMin, _blobRadiusMax, radiusT);

                    // Per-blob strength variation (0.7 to 1.0 of base strength)
                    float strengthVar = 0.7f + HashToFloat(cellHash ^ 0x87654321u) * 0.3f;
                    float strength = _blobStrength * strengthVar;

                    // Distance from sample point to blob center
                    float distX = x - blobX;
                    float distY = y - blobY;
                    float distSq = distX * distX + distY * distY;
                    float radiusSq = blobRadius * blobRadius;

                    // Early out if outside blob radius
                    if (distSq >= radiusSq) continue;

                    // Smooth falloff using quintic hermite: 1 at center, 0 at radius
                    float t = Mathf.Sqrt(distSq) / blobRadius;
                    float falloff = SmoothFalloff(t, _falloffPower);

                    totalContribution += falloff * strength;
                }
            }

            return totalContribution;
        }

        /// <summary>
        /// Quintic hermite smooth falloff: smooth at both center and edge.
        /// t: 0 at center, 1 at edge
        /// Returns: 1 at center, 0 at edge
        /// </summary>
        private static float SmoothFalloff(float t, float power)
        {
            t = Mathf.Clamp01(t);
            // Apply power curve before smoothstep for configurable sharpness
            float shaped = Mathf.Pow(t, 1f / power);
            // Quintic smoothstep: 6t^5 - 15t^4 + 10t^3 (inverted)
            float s = shaped;
            return 1f - (s * s * s * (s * (s * 6f - 15f) + 10f));
        }

        /// <summary>
        /// Deterministic 2D hash function for cell coordinates.
        /// </summary>
        private static uint Hash2D(int x, int y, float seed)
        {
            // Combine coordinates and seed into a single hash
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791f);
            // PCG-style mixing
            h = ((h >> 16) ^ h) * 0x45D9F3Bu;
            h = ((h >> 16) ^ h) * 0x45D9F3Bu;
            h = (h >> 16) ^ h;
            return h;
        }

        /// <summary>
        /// Convert a uint hash to a float in [0, 1).
        /// </summary>
        private static float HashToFloat(uint hash)
        {
            return (hash & 0xFFFFu) / 65536f;
        }
    }

    public struct MetaballSampleResult
    {
        /// <summary>Raw accumulated density from all contributing blobs.</summary>
        public float RawDensity;

        /// <summary>Density clamped to 0-1 range.</summary>
        public float NormalizedDensity;

        /// <summary>Whether the density exceeds the configured threshold.</summary>
        public bool IsActive;
    }
}
