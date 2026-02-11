using Unity.Entities;

namespace Starfire.Sim
{
    public struct AsteroidConfig : IComponentData
    {
        public float ReferenceSize;
        public float MinSizeFactor;
        public float SmallSizeThreshold;
        public float LargeSizeThreshold;
        public float Type0MaxSize;
        public float Type1MaxSize;
        public int TypeCount;

        public static byte ComputeTypeId(float size, byte composition, float type0Max, float type1Max)
        {
            byte sizeCategory = size < type0Max ? (byte)0 : size < type1Max ? (byte)1 : (byte)2;
            return (byte)(sizeCategory * 4 + composition);
        }
    }
}
