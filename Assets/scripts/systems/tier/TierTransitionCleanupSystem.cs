using Unity.Entities;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
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
