using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Starfire.Entity
{
    public struct AsteroidData : IComponentData
    {
        [GhostField(Quantization = 100)] public float Size;
        [GhostField] public byte Composition;
        [GhostField] public byte TypeId;
        [GhostField] public int ParentStarId;
        [GhostField(Quantization = 100)] public float2 OrbitalVelocity;
    }
}
