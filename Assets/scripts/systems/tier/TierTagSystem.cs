using Unity.Entities;
using Unity.Physics.Systems;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial class TierTagSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (transition, entity) in
                SystemAPI.Query<RefRO<TierTransition>>()
                    .WithPresent<TierTransition>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<TierTransition>(entity))
                    continue;

                switch (transition.ValueRO.NewTier)
                {
                    case SimulationTier.Loaded:
                        EntityManager.SetComponentEnabled<RichTierTag>(entity, true);
                        EntityManager.SetComponentEnabled<VisualTierTag>(entity, true);
                        EntityManager.SetComponentEnabled<SensorTierTag>(entity, false);
                        break;

                    case SimulationTier.Active:
                        EntityManager.SetComponentEnabled<RichTierTag>(entity, true);
                        EntityManager.SetComponentEnabled<VisualTierTag>(entity, false);
                        EntityManager.SetComponentEnabled<SensorTierTag>(entity, false);
                        break;

                    case SimulationTier.Sensor:
                        EntityManager.SetComponentEnabled<RichTierTag>(entity, false);
                        EntityManager.SetComponentEnabled<VisualTierTag>(entity, false);
                        EntityManager.SetComponentEnabled<SensorTierTag>(entity, true);
                        break;
                }
            }
        }
    }
}
