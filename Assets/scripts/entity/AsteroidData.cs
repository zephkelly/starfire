using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct AsteroidData : IComponentData
    {
        public float Size;
        public byte Composition;
        public float AngularSpeed;
        public float DriftSpeed;
        public float2 DriftDirection;
        public int ParentStarId;
    }
}
