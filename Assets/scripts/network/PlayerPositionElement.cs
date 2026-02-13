using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Network
{
    [InternalBufferCapacity(4)]
    public struct PlayerPositionElement : IBufferElementData
    {
        public int NetworkId;
        public double2 Position;
        public float SensorRange;
    }
}
