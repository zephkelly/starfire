using Unity.Entities;

namespace Starfire.Simulation
{
    public struct RichTierTag : IComponentData, IEnableableComponent {}

    public struct VisualTierTag : IComponentData, IEnableableComponent {}
    
    public struct SensorTierTag : IComponentData, IEnableableComponent {}

    public struct DormantTag : IComponentData { }

    public struct FleetTag : IComponentData { }

    public struct StarTag : IComponentData { }

    public struct AsteroidFieldTag : IComponentData { }
}