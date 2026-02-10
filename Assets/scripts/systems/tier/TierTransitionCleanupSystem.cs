using Unity.Entities;
using Unity.Physics.Systems;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierTagSystem))]
    [UpdateAfter(typeof(PhysicsInclusionSystem))]
    [UpdateAfter(typeof(RenderingStateSystem))]
    [UpdateAfter(typeof(ShipTierTransitionSystem))]
    public partial class TierTransitionCleanupSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (_, entity) in
                SystemAPI.Query<RefRO<TierTransition>>()
                    .WithPresent<TierTransition>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<TierTransition>(entity))
                    continue;

                EntityManager.SetComponentEnabled<TierTransition>(entity, false);
            }
        }
    }
}
