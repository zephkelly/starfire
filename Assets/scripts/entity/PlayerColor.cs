using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Starfire.Entity
{
    public struct PlayerColor : IComponentData
    {
        [GhostField] public float4 Value;
    }
}
