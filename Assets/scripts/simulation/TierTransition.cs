using Unity.Entities;

namespace Starfire.Simulation
{
    public struct TierTransition : IComponentData, IEnableableComponent
    {
        public SimulationTier PreviousTier;
        public SimulationTier NewTier;
    }
}
