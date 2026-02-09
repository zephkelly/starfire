using Unity.Entities;
 using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct WorldPosition : IComponentData
    {
        public double2 Value;
    }
}