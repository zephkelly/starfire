using Unity.Entities;
using UnityEngine;
using Starfire.Sim;

namespace Starfire.Authoring
{
    public class AsteroidConfigAuthoring : MonoBehaviour
    {
        public float referenceSize = 2.0f;
        public float minSizeFactor = 0.3f;
        public float smallSizeThreshold = 1.0f;
        public float largeSizeThreshold = 3.0f;
        public float type0MaxSize = 1.0f;
        public float type1MaxSize = 2.5f;
        public int typeCount = 12;

        class AsteroidConfigBaker : Baker<AsteroidConfigAuthoring>
        {
            public override void Bake(AsteroidConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new AsteroidConfig
                {
                    ReferenceSize = authoring.referenceSize,
                    MinSizeFactor = authoring.minSizeFactor,
                    SmallSizeThreshold = authoring.smallSizeThreshold,
                    LargeSizeThreshold = authoring.largeSizeThreshold,
                    Type0MaxSize = authoring.type0MaxSize,
                    Type1MaxSize = authoring.type1MaxSize,
                    TypeCount = authoring.typeCount
                });
            }
        }
    }
}
