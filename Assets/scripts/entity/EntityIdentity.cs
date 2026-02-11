using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Entity
{
    public struct EntityIdentity : IComponentData
    {
        [GhostField] public int Id;
        [GhostField] public byte EntityType;
        [GhostField] public byte Persistence;
        [GhostField] public int FactionId;
        [GhostField] public int ConfigId;
    }
}