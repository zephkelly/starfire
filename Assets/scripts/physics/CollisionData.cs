using Unity.Mathematics;

namespace Starfire.Physics
{
    public struct CollisionLayer
    {
        public uint Layer;
        public uint Mask;
        public byte IsTrigger;
    }

    public struct CollisionPair
    {
        public int IndexA;
        public int IndexB;
        public float OverlapDistance;
        public float2 Normal;
        public byte IsTrigger;
    }
}