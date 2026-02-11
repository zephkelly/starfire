using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Simulation
{
    public enum SimulationTier : byte
    {
        Loaded = 0,
        Active = 1,
        Sensor = 2,
        Strategic = 3,
        Dormant = 4
    }

    public struct SimulationTierData : IComponentData
    {
        [GhostField] public SimulationTier Tier;
        public float LastUpdatedTime;
    }
}