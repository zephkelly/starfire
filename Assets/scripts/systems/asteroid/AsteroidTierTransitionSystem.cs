using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial class AsteroidTierTransitionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (transition, entity) in
                SystemAPI.Query<RefRO<TierTransition>>()
                    .WithAll<AsteroidData>()
                    .WithEntityAccess())
            {
                var from = transition.ValueRO.PreviousTier;
                var to = transition.ValueRO.NewTier;

                if (from <= SimulationTier.Active && to == SimulationTier.Sensor)
                    CaptureVelocity(entity);
                else if (from == SimulationTier.Sensor && to <= SimulationTier.Active)
                    RestoreVelocity(entity);
            }
        }

        void CaptureVelocity(Unity.Entities.Entity entity)
        {
            var vel = EntityManager.GetComponentData<PhysicsVelocity>(entity);
            var data = EntityManager.GetComponentData<AsteroidData>(entity);
            data.OrbitalVelocity = vel.Linear.xy;
            EntityManager.SetComponentData(entity, data);
        }

        void RestoreVelocity(Unity.Entities.Entity entity)
        {
            var data = EntityManager.GetComponentData<AsteroidData>(entity);
            var vel = EntityManager.GetComponentData<PhysicsVelocity>(entity);
            vel.Linear = new float3(data.OrbitalVelocity.x, data.OrbitalVelocity.y, 0f);
            vel.Angular = float3.zero;
            EntityManager.SetComponentData(entity, vel);
        }
    }
}
