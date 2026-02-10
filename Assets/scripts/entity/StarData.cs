using Unity.Entities;

namespace Starfire.Entity
{
    public struct StarData : IComponentData
    {
        public byte SpectralType;
        public float Luminosity;
        public float Mass;
        public float Radius;
        public float SystemRadius;
        public uint Seed;
        public float GravityRange;
        public float GravityStrength;
        public float RadiationRadius;
    }
}
