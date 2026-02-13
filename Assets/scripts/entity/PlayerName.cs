using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Entity
{
    public struct PlayerName : IComponentData
    {
        [GhostField] public FixedString64Bytes Value;
    }
}
