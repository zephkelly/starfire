using System;
using UnityEngine;

namespace Starfire.Core.V2.World
{
    /// <summary>
    /// Double-precision 2D vector for tracking positions in large worlds.
    /// Avoids floating-point precision loss at distances far from origin.
    /// </summary>
    [Serializable]
    public struct Vector2D : IEquatable<Vector2D>
    {
        public double X;
        public double Y;

        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vector2D Zero => new Vector2D(0, 0);
        public static Vector2D One => new Vector2D(1, 1);

        public double Magnitude => Math.Sqrt(X * X + Y * Y);
        public double SqrMagnitude => X * X + Y * Y;

        public Vector2D Normalized
        {
            get
            {
                double mag = Magnitude;
                if (mag > 1e-10)
                    return new Vector2D(X / mag, Y / mag);
                return Zero;
            }
        }

        /// <summary>
        /// Convert to Unity Vector2 (loses precision at large values).
        /// </summary>
        public Vector2 ToVector2() => new Vector2((float)X, (float)Y);

        /// <summary>
        /// Convert to Unity Vector3 with Z = 0 (loses precision at large values).
        /// </summary>
        public Vector3 ToVector3() => new Vector3((float)X, (float)Y, 0f);

        /// <summary>
        /// Create from Unity Vector2.
        /// </summary>
        public static Vector2D FromVector2(Vector2 v) => new Vector2D(v.x, v.y);

        /// <summary>
        /// Create from Unity Vector3 (ignores Z).
        /// </summary>
        public static Vector2D FromVector3(Vector3 v) => new Vector2D(v.x, v.y);

        public static double Distance(Vector2D a, Vector2D b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Dot(Vector2D a, Vector2D b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static Vector2D Lerp(Vector2D a, Vector2D b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return new Vector2D(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t
            );
        }

        public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
        public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);
        public static Vector2D operator *(Vector2D a, double d) => new Vector2D(a.X * d, a.Y * d);
        public static Vector2D operator *(double d, Vector2D a) => new Vector2D(a.X * d, a.Y * d);
        public static Vector2D operator /(Vector2D a, double d) => new Vector2D(a.X / d, a.Y / d);
        public static Vector2D operator -(Vector2D a) => new Vector2D(-a.X, -a.Y);

        public static bool operator ==(Vector2D a, Vector2D b) => a.Equals(b);
        public static bool operator !=(Vector2D a, Vector2D b) => !a.Equals(b);

        public static implicit operator Vector2D(Vector2 v) => FromVector2(v);
        public static explicit operator Vector2(Vector2D v) => v.ToVector2();

        public bool Equals(Vector2D other)
        {
            return Math.Abs(X - other.X) < 1e-10 && Math.Abs(Y - other.Y) < 1e-10;
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2D other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString() => $"({X:F2}, {Y:F2})";
        public string ToString(string format) => $"({X.ToString(format)}, {Y.ToString(format)})";
    }
}
