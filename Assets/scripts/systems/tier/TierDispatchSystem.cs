using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using Starfire.Sim;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial class TierDispatchSystem : SystemBase
    {
        readonly List<ITierHandler> _handlers = new();

        public void Register(ITierHandler handler) => _handlers.Add(handler);

        protected override void OnCreate()
        {
            Register(new TierTagHandler());
            Register(new PhysicsTierHandler());

            RequireForUpdate<SimulationConfig>();
        }

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

                foreach (var handler in _handlers)
                    handler.OnTierChanged(entity, from, to, EntityManager, ecb);

                EntityManager.SetComponentEnabled<TierTransition>(entity, false);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
