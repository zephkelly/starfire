using Unity.Entities;
using UnityEngine;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Authoring
{
    public class StarAuthoring : MonoBehaviour
    {
        class StarBaker : Baker<StarAuthoring>
        {
            public override void Bake(StarAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new WorldPosition());
                AddComponent(entity, new EntityIdentity
                {
                    EntityType = (byte)Starfire.Entity.EntityType.Star,
                    Persistence = 2
                });
                AddComponent(entity, new SimulationTierData
                {
                    Tier = SimulationTier.Loaded
                });
                AddComponent(entity, new TierTransition());
                SetComponentEnabled<TierTransition>(entity, false);

                AddComponent<StarTag>(entity);
                AddComponent(entity, new StarData());

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
