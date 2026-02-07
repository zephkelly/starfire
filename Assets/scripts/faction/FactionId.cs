using System;

namespace Starfire.Faction
{
    public struct FactionId : IEquatable<FactionId>
    {
        public int Value;

        public FactionId(int value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return obj is FactionId other && Equals(other);
        }

        public bool Equals(FactionId other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(FactionId left, FactionId right) => left.Equals(right);
        public static bool operator !=(FactionId left, FactionId right) => !left.Equals(right);
    }
}