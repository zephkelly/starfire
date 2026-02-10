using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct AsteroidData : IComponentData
    {
        public float Size;
        public byte Composition;
        public int ParentStarId;
        public float2 OrbitalVelocity;
    }
}
