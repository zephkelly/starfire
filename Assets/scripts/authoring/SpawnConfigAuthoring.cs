using Unity.Entities;
using UnityEngine;
using Starfire.Sim;

namespace Starfire.Authoring
{
    public class SpawnConfigAuthoring : MonoBehaviour
    {
        [Header("Entity Counts")]
        public int tier0Ships = 10;
        public int tier1Ships = 200;
        public int tier2Ships = 500;
        public int tier3Fleets = 20;
        public int tier3MembersPerFleet = 10;
        public int tier4Dormant = 50;

        [Header("Celestial Generation")]
        public uint worldSeed = 12345;
        public int targetStarCount = 1000;
        public int targetAsteroidCount = 100000;

        [Header("Ship Defaults")]
        public float defaultMaxSpeed = 200f;
        public float defaultAcceleration = 60f;
        public float defaultTurnRate = 180f;
        public float defaultMaxHealth = 100f;
        public float shipColliderRadius = 0.5f;
        public float shipMass = 50f;

        [Header("Sensor Ranges")]
        public float playerSensorRange = 8000f;
        public float smallShipSensorRange = 1000f;
        public float mediumShipSensorRange = 3000f;
        public float largeShipSensorRange = 8000f;

        class SpawnConfigBaker : Baker<SpawnConfigAuthoring>
        {
            public override void Bake(SpawnConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new SpawnConfig
                {
                    Tier0Ships = authoring.tier0Ships,
                    Tier1Ships = authoring.tier1Ships,
                    Tier2Ships = authoring.tier2Ships,
                    Tier3Fleets = authoring.tier3Fleets,
                    Tier3MembersPerFleet = authoring.tier3MembersPerFleet,
                    Tier4Dormant = authoring.tier4Dormant,
                    WorldSeed = authoring.worldSeed,
                    TargetStarCount = authoring.targetStarCount,
                    TargetAsteroidCount = authoring.targetAsteroidCount,
                    DefaultMaxSpeed = authoring.defaultMaxSpeed,
                    DefaultAcceleration = authoring.defaultAcceleration,
                    DefaultTurnRate = authoring.defaultTurnRate,
                    DefaultMaxHealth = authoring.defaultMaxHealth,
                    ShipColliderRadius = authoring.shipColliderRadius,
                    ShipMass = authoring.shipMass,
                    PlayerSensorRange = authoring.playerSensorRange,
                    SmallShipSensorRange = authoring.smallShipSensorRange,
                    MediumShipSensorRange = authoring.mediumShipSensorRange,
                    LargeShipSensorRange = authoring.largeShipSensorRange
                });
            }
        }
    }
}
