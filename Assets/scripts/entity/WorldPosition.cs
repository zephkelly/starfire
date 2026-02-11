using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Starfire.Entity
{
    public struct WorldPosition : IComponentData
    {
        [GhostField(Quantization = 0)]
        public double2 Value;
    }
}