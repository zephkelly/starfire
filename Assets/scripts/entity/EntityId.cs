using System;

namespace Starfire.Entity
{
    public struct EntityId : IEquatable<EntityId>
    {
        public int Value;
        public EntityType Type;

        public EntityId(int value, EntityType type)
        {
            Value = value;
            Type = type;
        }

        public bool Equals(EntityId other)
        {
            return Value == other.Value && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Type);
        }

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
    }
}