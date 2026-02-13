using Unity.Entities;

namespace Starfire.Sim
{
    public struct SpawnConfig : IComponentData
    {
        public int Tier0Ships;
        public int Tier1Ships;
        public int Tier2Ships;
        public int Tier3Fleets;
        public int Tier3MembersPerFleet;
        public int Tier4Dormant;

        public uint WorldSeed;
        public int TargetStarCount;
        public int TargetAsteroidCount;

        public float DefaultMaxSpeed;
        public float DefaultAcceleration;
        public float DefaultTurnRate;
        public float DefaultMaxHealth;

        public float PlayerMaxSpeed;
        public float PlayerAcceleration;
        public float ShipColliderRadius;
        public float ShipMass;

        public float PlayerSensorRange;
        public float SmallShipSensorRange;
        public float MediumShipSensorRange;
        public float LargeShipSensorRange;
    }
}
