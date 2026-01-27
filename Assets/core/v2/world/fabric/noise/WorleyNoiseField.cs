using System;
using UnityEngine;
using Starfire.Core.V2.World;

namespace StarfireV2
{
    /// <summary>
    /// Worley/cellular noise for hazard zones and blob-like features.
    /// </summary>
    [Serializable]
    public class WorleyNoiseField : INoiseField
    {
        [SerializeField] private float frequency = 0.0005f;
        [SerializeField] private WorleyDistanceFunction distanceFunction = WorleyDistanceFunction.Euclidean;
        [SerializeField] private WorleyReturnType returnType = WorleyReturnType.F1;
        [Range(1, 4)]
        [SerializeField] private int pointsPerCell = 1;

        public float Frequency { get => frequency; set => frequency = value; }
        public WorleyDistanceFunction DistanceFunction { get => distanceFunction; set => distanceFunction = value; }
        public WorleyReturnType ReturnType { get => returnType; set => returnType = value; }

        public WorleyNoiseField() { }

        public WorleyNoiseField(float frequency, WorleyReturnType returnType = WorleyReturnType.F1)
        {
            this.frequency = frequency;
            this.returnType = returnType;
        }

        public float Sample(Vector2D position)
        {
            return SampleInternal((float)position.X, (float)position.Y, 0f);
        }

        public float Sample(Vector2D position, float seed)
        {
            return SampleInternal((float)position.X, (float)position.Y, seed);
        }

        private float SampleInternal(float x, float y, float seed)
        {
            float scaledX = x * frequency;
            float scaledY = y * frequency;

            int cellX = (int)Math.Floor(scaledX);
            int cellY = (int)Math.Floor(scaledY);

            float f1 = float.MaxValue;
            float f2 = float.MaxValue;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = cellX + dx;
                    int ny = cellY + dy;

                    for (int p = 0; p < pointsPerCell; p++)
                    {
                        Vector2 point = GetFeaturePoint(nx, ny, p, seed);
                        float dist = CalculateDistance(scaledX, scaledY, point.x, point.y);

                        if (dist < f1)
                        {
                            f2 = f1;
                            f1 = dist;
                        }
                        else if (dist < f2)
                        {
                            f2 = dist;
                        }
                    }
                }
            }

            float result = returnType switch
            {
                WorleyReturnType.F1 => f1,
                WorleyReturnType.F2 => f2,
                WorleyReturnType.F2MinusF1 => f2 - f1,
                WorleyReturnType.F1PlusF2 => (f1 + f2) * 0.5f,
                _ => f1
            };

            return Mathf.Clamp01(result);
        }

        private Vector2 GetFeaturePoint(int cellX, int cellY, int pointIndex, float seed)
        {
            int hash = Hash(cellX, cellY, pointIndex, seed);

            float px = cellX + HashToFloat(hash);
            float py = cellY + HashToFloat(hash * 16807 + 1);

            return new Vector2(px, py);
        }

        private float CalculateDistance(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;

            return distanceFunction switch
            {
                WorleyDistanceFunction.Euclidean => Mathf.Sqrt(dx * dx + dy * dy),
                WorleyDistanceFunction.Manhattan => Mathf.Abs(dx) + Mathf.Abs(dy),
                WorleyDistanceFunction.Chebyshev => Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)),
                _ => Mathf.Sqrt(dx * dx + dy * dy)
            };
        }

        private static int Hash(int x, int y, int p, float seed)
        {
            int hash = 17;
            hash = hash * 31 + x;
            hash = hash * 31 + y;
            hash = hash * 31 + p;
            hash = hash * 31 + (int)(seed * 1000);
            return hash & 0x7FFFFFFF;
        }

        private static float HashToFloat(int hash)
        {
            return (hash & 0xFFFF) / 65536f;
        }
    }

    public enum WorleyDistanceFunction { Euclidean, Manhattan, Chebyshev }
    public enum WorleyReturnType { F1, F2, F2MinusF1, F1PlusF2 }
}
