using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
using Starfire.Simulation;

namespace Starfire.Systems
{
    public class PhysicsTierHandler : ITierHandler
    {
        public void OnTierChanged(Unity.Entities.Entity entity, SimulationTier from, SimulationTier to,
            EntityManager em, EntityCommandBuffer ecb)
        {
            if (to == SimulationTier.Sensor)
            {
                if (em.HasComponent<PhysicsWorldIndex>(entity))
                    ecb.RemoveComponent<PhysicsWorldIndex>(entity);
                if (!em.HasComponent<DisableRendering>(entity))
                    ecb.AddComponent<DisableRendering>(entity);
                return;
            }

            if (from == SimulationTier.Sensor && to <= SimulationTier.Active)
                ecb.AddSharedComponent(entity, new PhysicsWorldIndex());

            if (to == SimulationTier.Loaded)
            {
                ecb.RemoveComponent<DisableRendering>(entity);
            }
            else if (to == SimulationTier.Active && from == SimulationTier.Loaded)
            {
                if (!em.HasComponent<DisableRendering>(entity))
                    ecb.AddComponent<DisableRendering>(entity);
            }
        }
    }
}
