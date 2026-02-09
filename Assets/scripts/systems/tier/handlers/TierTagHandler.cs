using Unity.Entities;
using Starfire.Simulation;

namespace Starfire.Systems
{
    public class TierTagHandler : ITierHandler
    {
        public void OnTierChanged(Unity.Entities.Entity entity, SimulationTier from, SimulationTier to,
            EntityManager em, EntityCommandBuffer ecb)
        {
            switch (to)
            {
                case SimulationTier.Loaded:
                    em.SetComponentEnabled<RichTierTag>(entity, true);
                    em.SetComponentEnabled<VisualTierTag>(entity, true);
                    em.SetComponentEnabled<SensorTierTag>(entity, false);
                    break;

                case SimulationTier.Active:
                    em.SetComponentEnabled<RichTierTag>(entity, true);
                    em.SetComponentEnabled<VisualTierTag>(entity, false);
                    em.SetComponentEnabled<SensorTierTag>(entity, false);
                    break;

                case SimulationTier.Sensor:
                    em.SetComponentEnabled<RichTierTag>(entity, false);
                    em.SetComponentEnabled<VisualTierTag>(entity, false);
                    em.SetComponentEnabled<SensorTierTag>(entity, true);
                    break;
            }
        }
    }
}
