using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public struct Vector2D
    {
        public double x;
        public double y;

        public Vector2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2D operator +(Vector2D a, Vector2D b)
        {
            return new Vector2D(a.x + b.x, a.y + b.y);
        }

        public static Vector2D operator -(Vector2D a, Vector2D b)
        {
            return new Vector2D(a.x - b.x, a.y - b.y);
        }

        public static Vector2D operator *(Vector2D a, double b)
        {
            return new Vector2D(a.x * b, a.y * b);
        }

        public static Vector2D operator /(Vector2D a, double b)
        {
            return new Vector2D(a.x / b, a.y / b);
        }

        public static bool operator ==(Vector2D a, Vector2D b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Vector2D a, Vector2D b)
        {
            return a.x != b.x || a.y != b.y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Vector2D v = (Vector2D)obj;
            return x == v.x && y == v.y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2((float)x, (float)y);
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }

        public override string ToString()
        {
            return "(" + x + ", " + y + ")";
        }
    }
}