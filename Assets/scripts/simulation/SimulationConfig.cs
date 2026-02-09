using Unity.Entities;

namespace Starfire.Sim
{
    public struct SimulationConfig : IComponentData
    {
        public TierBounds Bounds;
        public TierCapacity Capacity;
        public FleetSettings Fleet;
        public SensorSettings Sensor;
    }

    public struct TierBounds
    {
        public float Tier0MaxDistance;
        public float Tier1MaxDistance;
        public float Tier2MaxDistance;
        public float Tier3MaxDistance;
        public float Tier0Hysteresis;
        public float Tier1Hysteresis;
        public float Tier2Hysteresis;
        public float Tier3Hysteresis;
        public float TierChangeCooldown;
        public float HysteresisPercent;
    }

    public struct TierCapacity
    {
        public int Tier0MaxEntities;
        public int RichLayerMaxEntities;
        public int SensorLayerMaxEntities;
        public int MaxFleets;
        public int MaxCriticalEntities;
    }

    public struct FleetSettings
    {
        public int MinFleetSize;
        public float GroupingRadius;
    }

    public struct SensorSettings
    {
        public float DefaultRange;
        public float EngagementBuffer;
        public int UpdateBatchSize;
    }
}
