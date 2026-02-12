using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Starfire.Entity;

namespace Starfire.Authoring
{
    public class WorldOriginAuthoring : MonoBehaviour
    {
        public float rebaseThreshold = 10000f;
        public float worldBoundsRadius = 500000f;

        class OriginBaker : Baker<WorldOriginAuthoring>
        {
            public override void Bake(WorldOriginAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new WorldOrigin
                {
                    Value = double2.zero,
                    RebaseThreshold = authoring.rebaseThreshold,
                    WorldBoundsRadius = authoring.worldBoundsRadius
                });
            }
        }
    }
}
