using Unity.Entities;

namespace Starfire.Entity
{
    public struct EntityIdentity : IComponentData
    {
        public int Id;
        public byte EntityType;
        public byte Persistence;
        public int FactionId;
        public int ConfigId;
    }
}