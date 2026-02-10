using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Rendering;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial class RenderingStateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transition, entity) in
                SystemAPI.Query<RefRO<TierTransition>>()
                    .WithPresent<TierTransition>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<TierTransition>(entity))
                    continue;

                var from = transition.ValueRO.PreviousTier;
                var to = transition.ValueRO.NewTier;

                if (to == SimulationTier.Sensor)
                {
                    if (!EntityManager.HasComponent<DisableRendering>(entity))
                        ecb.AddComponent<DisableRendering>(entity);
                }
                else if (to == SimulationTier.Loaded)
                {
                    ecb.RemoveComponent<DisableRendering>(entity);
                }
                else if (to == SimulationTier.Active && from == SimulationTier.Loaded)
                {
                    if (!EntityManager.HasComponent<DisableRendering>(entity))
                        ecb.AddComponent<DisableRendering>(entity);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
