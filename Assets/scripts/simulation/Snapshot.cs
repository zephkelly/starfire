using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct ShipSnapshot : IComponentData
    {
        public int ConfigId;
        public int FactionIndex;
        public byte Persistence;
        public float HullPercent;
        public float ShieldPercent;
        public float PropulsionEfficiency;
        public float RotationEfficiency;
        public double2 Position;
        public double2 Velocity;
        public float Heading;
        public byte AIState;               // SensorAIState
        public int TargetEntityId;
        public double2 Waypoint;
    }
}