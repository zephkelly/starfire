using System;
using UnityEngine;
using Unity.Mathematics;

namespace Starfire.Core
{
    public struct AbsolutePosition
    {
        public double X;
        public double Y;

        public AbsolutePosition(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double DistanceTo(AbsolutePosition other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Magnitude(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        public float2 ToFloat2()
        {
            return new float2((float)X, (float)Y);
        }
    }
}