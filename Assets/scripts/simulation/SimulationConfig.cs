using Unity.Entities;

namespace Starfire.Sim
{
    public struct SimulationConfig : IComponentData
    {
        // Tier boundaries
        public float Tier0MaxDistance;      // 5000
        public float Tier1MaxDistance;      // 20000
        public float Tier2MaxDistance;      // 100000
        public float Tier3MaxDistance;      // 200000

        // Hysteresis (15%)
        public float Tier0Hysteresis;      // 750
        public float Tier1Hysteresis;      // 3000
        public float Tier2Hysteresis;      // 15000
        public float Tier3Hysteresis;      // 30000

        // Cooldowns
        public float TierChangeCooldown;   // 2.0

        // Capacities
        public int Tier0MaxEntities;       // 20
        public int RichLayerMaxEntities;   // 500
        public int SensorLayerMaxEntities; // 2000
        public int MaxFleets;              // 100
        public int MaxCriticalEntities;    // 50

        // Sensor batch
        public int SensorUpdateBatchSize;  // 30

        // Fleet
        public int MinFleetSize;           // 3
        public float FleetGroupingRadius;  // 5000

        // Variable simulation distances
        public float DefaultSensorRange;   // 2000 (fallback when SensorRange == 0)
        public float EngagementBuffer;     // 2000 (extra distance beyond detection before tier demotion)
        public float HysteresisPercent;    // 0.15 (15% of computed boundary)
    }
}