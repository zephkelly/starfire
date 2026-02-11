using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    [InternalBufferCapacity(8)]
    public struct AsteroidFieldMember : IBufferElementData
    {
        public int EntityId;
        public double2 Position;
        public float Size;
        public byte Composition;
        public byte TypeId;
        public float2 OrbitalVelocity;
    }
}
