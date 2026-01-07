using UnityEngine;

namespace Starfire.Core.Noise
{
    /// <summary>
    /// Utility class for procedural noise generation.
    /// Used by shooting stars, nebulae, and other procedural effects.
    /// </summary>
    public static class NoiseUtility
    {
        // Permutation table for Perlin noise
        private static readonly int[] Permutation = {
            151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,
            8,99,37,240,21,10,23,190,6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,
            35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,74,165,71,
            134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,
            55,46,245,40,244,102,143,54,65,25,63,161,1,216,80,73,209,76,132,187,208,89,
            18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,52,217,226,
            250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,
            189,28,42,223,183,170,213,119,248,152,2,44,154,163,70,221,153,101,155,167,43,
            172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,218,246,97,
            228,251,34,242,193,238,210,144,12,191,179,162,241,81,51,145,235,249,14,239,
            107,49,192,214,31,181,199,106,157,184,84,204,176,115,121,50,45,127,4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
        };

        private static readonly int[] P;

        static NoiseUtility()
        {
            P = new int[512];
            for (int i = 0; i < 256; i++)
            {
                P[i] = Permutation[i];
                P[256 + i] = Permutation[i];
            }
        }

        /// <summary>
        /// Classic 2D Perlin noise.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Noise value between -1 and 1</returns>
        public static float Perlin2D(float x, float y)
        {
            int xi = (int)Mathf.Floor(x) & 255;
            int yi = (int)Mathf.Floor(y) & 255;

            float xf = x - Mathf.Floor(x);
            float yf = y - Mathf.Floor(y);

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = P[P[xi] + yi];
            int ab = P[P[xi] + yi + 1];
            int ba = P[P[xi + 1] + yi];
            int bb = P[P[xi + 1] + yi + 1];

            float x1 = Mathf.Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            float x2 = Mathf.Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

            return Mathf.Lerp(x1, x2, v);
        }

        /// <summary>
        /// Scaled 2D Perlin noise.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="scale">Scale factor (smaller = larger features)</param>
        /// <returns>Noise value between -1 and 1</returns>
        public static float Perlin2D(float x, float y, float scale)
        {
            return Perlin2D(x * scale, y * scale);
        }

        /// <summary>
        /// Scaled 2D Perlin noise with Vector2 input.
        /// </summary>
        public static float Perlin2D(Vector2 pos, float scale)
        {
            return Perlin2D(pos.x * scale, pos.y * scale);
        }

        /// <summary>
        /// Fractal Brownian Motion - layered noise for more natural patterns.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="octaves">Number of noise layers (more = more detail)</param>
        /// <param name="persistence">How much each octave contributes (0.5 typical)</param>
        /// <param name="scale">Base scale</param>
        /// <returns>Noise value (range depends on octaves)</returns>
        public static float FBM(float x, float y, int octaves, float persistence, float scale = 1f)
        {
            float total = 0f;
            float frequency = scale;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                total += Perlin2D(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }

            return total / maxValue;
        }

        /// <summary>
        /// Sample a direction from a noise field.
        /// Useful for shooting star directions that vary by region.
        /// </summary>
        /// <param name="worldPos">World position to sample</param>
        /// <param name="scale">Noise scale (smaller = larger regions with same direction)</param>
        /// <param name="baseAngle">Base angle in degrees (0 = right, 90 = up)</param>
        /// <param name="influence">How much noise affects direction (0-1)</param>
        /// <returns>Angle in degrees</returns>
        public static float SampleDirection(Vector2 worldPos, float scale, float baseAngle, float influence)
        {
            float noise = Perlin2D(worldPos, scale);
            float noiseAngle = noise * 180f * influence;
            return baseAngle + noiseAngle;
        }

        /// <summary>
        /// Get a direction vector from noise.
        /// </summary>
        public static Vector2 SampleDirectionVector(Vector2 worldPos, float scale, float baseAngle, float influence)
        {
            float angle = SampleDirection(worldPos, scale, baseAngle, influence) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// Normalized noise (0 to 1 range).
        /// </summary>
        public static float Perlin2DNormalized(float x, float y)
        {
            return (Perlin2D(x, y) + 1f) * 0.5f;
        }

        /// <summary>
        /// Normalized noise with scale.
        /// </summary>
        public static float Perlin2DNormalized(float x, float y, float scale)
        {
            return (Perlin2D(x, y, scale) + 1f) * 0.5f;
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 7;
            float u = h < 4 ? x : y;
            float v = h < 4 ? y : x;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2f * v : 2f * v);
        }
    }
}
