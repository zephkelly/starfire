using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial class TierStateTransitionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            bool hasStructuralChanges = false;

            foreach (var (transition, entity) in
                SystemAPI.Query<RefRO<TierTransition>>()
                    .WithEntityAccess())
            {
                var from = transition.ValueRO.PreviousTier;
                var to = transition.ValueRO.NewTier;

                switch (to)
                {
                    case SimulationTier.Loaded:
                        EntityManager.SetComponentEnabled<RichTierTag>(entity, true);
                        EntityManager.SetComponentEnabled<VisualTierTag>(entity, true);
                        EntityManager.SetComponentEnabled<SensorTierTag>(entity, false);
                        ecb.RemoveComponent<DisableRendering>(entity);
                        if (from == SimulationTier.Sensor)
                            ecb.AddSharedComponent(entity, new PhysicsWorldIndex());
                        hasStructuralChanges = true;
                        break;

                    case SimulationTier.Active:
                        EntityManager.SetComponentEnabled<RichTierTag>(entity, true);
                        EntityManager.SetComponentEnabled<VisualTierTag>(entity, false);
                        EntityManager.SetComponentEnabled<SensorTierTag>(entity, false);
                        if (from == SimulationTier.Loaded)
                            ecb.AddComponent<DisableRendering>(entity);
                        if (from == SimulationTier.Sensor)
                            ecb.AddSharedComponent(entity, new PhysicsWorldIndex());
                        hasStructuralChanges = true;
                        break;

                    case SimulationTier.Sensor:
                        EntityManager.SetComponentEnabled<RichTierTag>(entity, false);
                        EntityManager.SetComponentEnabled<VisualTierTag>(entity, false);
                        EntityManager.SetComponentEnabled<SensorTierTag>(entity, true);
                        ecb.RemoveComponent<PhysicsWorldIndex>(entity);
                        ecb.AddComponent<DisableRendering>(entity);
                        hasStructuralChanges = true;
                        break;
                }
            }

            if (hasStructuralChanges)
            {
                ecb.Playback(EntityManager);
            }
            ecb.Dispose();
        }
    }
}
