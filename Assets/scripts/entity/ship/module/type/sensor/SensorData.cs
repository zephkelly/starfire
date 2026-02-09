using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct SensorContact : IComponentData
    {
        public float HullPercent;
        public float ShieldPercent;
        public float Speed;
        public float Heading;
        public float MaxSpeed;
        public float WeaponRange;
        public float SensorRange;
        public float CombatStrength;
        public byte CurrentAIState;        // SensorAIState enum
        public byte PreviousAIState;
        public float StateTimer;
        public int TargetEntityId;
        public double2 Waypoint;
        public byte ShieldsActive;
        public byte WeaponsArmed;
    }
}