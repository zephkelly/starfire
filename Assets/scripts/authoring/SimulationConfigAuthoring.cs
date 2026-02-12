using Unity.Entities;
using UnityEngine;
using Starfire.Sim;

namespace Starfire.Authoring
{
    public class SimulationConfigAuthoring : MonoBehaviour
    {
        [Header("Tier Distances")]
        public float tier0MaxDistance = 5000f;
        public float tier1MaxDistance = 20000f;
        public float tier2MaxDistance = 100000f;
        public float tier3MaxDistance = 200000f;
        public float hysteresisPercent = 0.15f;
        public float tierChangeCooldown = 2f;

        [Header("Capacity")]
        public int tier0MaxEntities = 20;
        public int richLayerMaxEntities = 500;
        public int sensorLayerMaxEntities = 2000;
        public int maxFleets = 100;
        public int maxCriticalEntities = 50;

        [Header("Fleet")]
        public int minFleetSize = 3;
        public float groupingRadius = 5000f;

        [Header("Sensor")]
        public float defaultRange = 2000f;
        public float engagementBuffer = 2000f;
        public int updateBatchSize = 30;

        class SimConfigBaker : Baker<SimulationConfigAuthoring>
        {
            public override void Bake(SimulationConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new SimulationConfig
                {
                    Bounds = new TierBounds
                    {
                        Tier0MaxDistance = authoring.tier0MaxDistance,
                        Tier1MaxDistance = authoring.tier1MaxDistance,
                        Tier2MaxDistance = authoring.tier2MaxDistance,
                        Tier3MaxDistance = authoring.tier3MaxDistance,
                        Tier0Hysteresis = authoring.tier0MaxDistance * authoring.hysteresisPercent,
                        Tier1Hysteresis = authoring.tier1MaxDistance * authoring.hysteresisPercent,
                        Tier2Hysteresis = authoring.tier2MaxDistance * authoring.hysteresisPercent,
                        Tier3Hysteresis = authoring.tier3MaxDistance * authoring.hysteresisPercent,
                        TierChangeCooldown = authoring.tierChangeCooldown,
                        HysteresisPercent = authoring.hysteresisPercent
                    },
                    Capacity = new TierCapacity
                    {
                        Tier0MaxEntities = authoring.tier0MaxEntities,
                        RichLayerMaxEntities = authoring.richLayerMaxEntities,
                        SensorLayerMaxEntities = authoring.sensorLayerMaxEntities,
                        MaxFleets = authoring.maxFleets,
                        MaxCriticalEntities = authoring.maxCriticalEntities
                    },
                    Fleet = new FleetSettings
                    {
                        MinFleetSize = authoring.minFleetSize,
                        GroupingRadius = authoring.groupingRadius
                    },
                    Sensor = new SensorSettings
                    {
                        DefaultRange = authoring.defaultRange,
                        EngagementBuffer = authoring.engagementBuffer,
                        UpdateBatchSize = authoring.updateBatchSize
                    }
                });
            }
        }
    }
}
