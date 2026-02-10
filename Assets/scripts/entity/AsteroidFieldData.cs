using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct AsteroidFieldData : IComponentData
    {
        public double2 Position;
        public float Radius;
        public int Count;
        public byte DominantComposition;
        public float TotalMass;
        public uint Seed;
        public int ParentStarId;
        public float LastUpdateTime;
    }
}
