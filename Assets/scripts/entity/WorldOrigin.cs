using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct WorldOrigin : IComponentData
    {
        public float WorldBoundsRadius;
    }
}