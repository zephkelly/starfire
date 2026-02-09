using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    [InternalBufferCapacity(8)]
    public struct FleetMember : IBufferElementData
    {
        public int EntityId;
        public int FormationSlot;
        public float2 FormationOffset;
        public float Strength;
        public float HP;
        public byte Persistence;
        public ShipSnapshot Snapshot;
    }
}