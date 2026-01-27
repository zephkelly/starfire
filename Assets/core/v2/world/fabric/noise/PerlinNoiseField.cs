using System;
using UnityEngine;
using Starfire.Core.Noise;
using Starfire.Core.V2.World;

namespace StarfireV2
{
    /// <summary>
    /// Multi-octave Perlin noise field for density fields and terrain-like features.
    /// </summary>
    [Serializable]
    public class PerlinNoiseField : INoiseField
    {
        [SerializeField] private float scale = 0.001f;
        [Range(1, 8)]
        [SerializeField] private int octaves = 4;
        [Range(0.1f, 0.9f)]
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2f;
        [SerializeField] private Vector2 offset = Vector2.zero;

        public float Scale { get => scale; set => scale = value; }
        public int Octaves { get => octaves; set => octaves = Mathf.Clamp(value, 1, 8); }
        public float Persistence { get => persistence; set => persistence = Mathf.Clamp(value, 0.1f, 0.9f); }
        public float Lacunarity { get => lacunarity; set => lacunarity = value; }
        public Vector2 Offset { get => offset; set => offset = value; }

        public PerlinNoiseField() { }

        public PerlinNoiseField(float scale, int octaves = 4, float persistence = 0.5f)
        {
            this.scale = scale;
            this.octaves = octaves;
            this.persistence = persistence;
        }

        public float Sample(Vector2D position)
        {
            return SampleInternal((float)position.X + offset.x, (float)position.Y + offset.y);
        }

        public float Sample(Vector2D position, float seed)
        {
            float seedOffset = seed * 10000f;
            return SampleInternal(
                (float)position.X + offset.x + seedOffset,
                (float)position.Y + offset.y + seedOffset
            );
        }

        private float SampleInternal(float x, float y)
        {
            float total = 0f;
            float frequency = scale;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                total += NoiseUtility.Perlin2D(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            float result = total / maxValue;
            return (result + 1f) * 0.5f;
        }
    }
}
