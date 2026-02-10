using Unity.Entities;
using Unity.Physics.Systems;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierStateTransitionSystem))]
    [UpdateAfter(typeof(ShipTierTransitionSystem))]
    [UpdateAfter(typeof(AsteroidTierTransitionSystem))]
    public partial class TierTransitionCleanupSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (_, entity) in
                SystemAPI.Query<RefRO<TierTransition>>()
                    .WithEntityAccess())
            {
                EntityManager.SetComponentEnabled<TierTransition>(entity, false);
            }
        }
    }
}
