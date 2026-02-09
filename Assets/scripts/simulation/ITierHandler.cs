using Unity.Entities;

namespace Starfire.Simulation
{
    public interface ITierHandler
    {
        void OnTierChanged(Unity.Entities.Entity entity, SimulationTier from, SimulationTier to,
            EntityManager em, EntityCommandBuffer ecb);
    }
}
