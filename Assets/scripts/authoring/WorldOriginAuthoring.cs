using Unity.Entities;
using UnityEngine;
using Starfire.Entity;

namespace Starfire.Authoring
{
    public class WorldOriginAuthoring : MonoBehaviour
    {
        public float worldBoundsRadius = 500000f;

        class OriginBaker : Baker<WorldOriginAuthoring>
        {
            public override void Bake(WorldOriginAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new WorldOrigin
                {
                    WorldBoundsRadius = authoring.worldBoundsRadius
                });
            }
        }
    }
}
