using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Authoring
{
    public class AsteroidAuthoring : MonoBehaviour
    {
        class AsteroidBaker : Baker<AsteroidAuthoring>
        {
            public override void Bake(AsteroidAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new WorldPosition());
                AddComponent(entity, new EntityIdentity
                {
                    EntityType = (byte)Starfire.Entity.EntityType.Asteroid
                });
                AddComponent(entity, new SimulationTierData
                {
                    Tier = SimulationTier.Loaded
                });
                AddComponent(entity, new TierTransition());
                SetComponentEnabled<TierTransition>(entity, false);

                AddComponent<AsteroidTag>(entity);
                AddComponent(entity, new AsteroidData());

                AddComponent(entity, new SensorContact
                {
                    HullPercent = 1f
                });

                AddComponent(entity, new RichTierTag());
                SetComponentEnabled<RichTierTag>(entity, true);
                AddComponent(entity, new VisualTierTag());
                SetComponentEnabled<VisualTierTag>(entity, true);
                AddComponent(entity, new SensorTierTag());
                SetComponentEnabled<SensorTierTag>(entity, false);
            }
        }
    }
}
