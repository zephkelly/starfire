using Unity.Entities;
using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct FleetData : IComponentData
    {
        public int FleetId;
        public int FactionIndex;
        public double2 Position;
        public double2 Velocity;
        public float Rotation;
        public int MemberCount;
        public byte Formation;             // FleetFormation enum
        public float TotalStrength;
        public float TotalHP;
        public byte CurrentBehavior;       // FleetBehavior enum
        public int TargetFleetId;
        public float LastUpdateTime;
    }
}