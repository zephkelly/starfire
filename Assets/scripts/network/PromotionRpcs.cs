using Unity.Mathematics;
using Unity.NetCode;

namespace Starfire.Network
{
    public struct EntityPromotedRpc : IRpcCommand
    {
        public int EntityId;
    }

    public struct EntityDemotedRpc : IRpcCommand
    {
        public int EntityId;
        public byte EntityType;
        public double2 Position;
        public float2 Velocity;
        public float2 OrbitalVelocity;
        public float Health;
        public byte Tier;
    }
}
