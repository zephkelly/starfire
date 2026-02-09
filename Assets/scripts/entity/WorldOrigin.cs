using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct WorldOrigin : IComponentData
    {
        public double2 Value;
        public float RebaseThreshold;
        public float WorldBoundsRadius;
    }
}