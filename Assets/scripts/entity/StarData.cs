using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Entity
{
    public struct StarData : IComponentData
    {
        [GhostField] public byte SpectralType;
        [GhostField(Quantization = 100)] public float Luminosity;
        [GhostField(Quantization = 100)] public float Mass;
        [GhostField(Quantization = 100)] public float Radius;
        [GhostField(Quantization = 100)] public float SystemRadius;
        [GhostField] public uint Seed;
        [GhostField(Quantization = 100)] public float GravityRange;
        [GhostField(Quantization = 100)] public float GravityStrength;
        [GhostField(Quantization = 100)] public float RadiationRadius;
    }
}
