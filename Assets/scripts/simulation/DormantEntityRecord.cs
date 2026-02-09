using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct DormantRecord : IComponentData
    {
        public long ChunkX;
        public long ChunkY;
        public byte EntityType;
        public int FactionIndex;
        public int Count;                  // for grouped transients
        public uint Seed;                  // for procedural regeneration
        public byte Persistence;
        public ShipSnapshot Snapshot;      // only for Persistent entities
    }
}