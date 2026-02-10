using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial class PhysicsInclusionSystem : SystemBase
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
                    if (EntityManager.HasComponent<PhysicsWorldIndex>(entity))
                        ecb.RemoveComponent<PhysicsWorldIndex>(entity);
                }
                else if (from == SimulationTier.Sensor && to <= SimulationTier.Active)
                {
                    ecb.AddSharedComponent(entity, new PhysicsWorldIndex());
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
