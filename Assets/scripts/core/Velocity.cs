using System;
using Unity.Mathematics;

namespace Starfire.Core
{
    public struct Velocity
    {
        public double X;
        public double Y;

        public double Magnitude => Math.Sqrt(X * X + Y * Y);
        public double Direction => Math.Atan2(Y, X);

        public Velocity(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator double2(Velocity vel) => new double2(vel.X, vel.Y);
    }
}