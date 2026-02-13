using Unity.Mathematics;
using Unity.NetCode;

namespace Starfire.Network
{
    public struct PlayerPositionBroadcastRpc : IRpcCommand
    {
        public int PlayerCount;
        public int NetId0;
        public double2 Pos0;
        public float Range0;
        public int NetId1;
        public double2 Pos1;
        public float Range1;
        public int NetId2;
        public double2 Pos2;
        public float Range2;
        public int NetId3;
        public double2 Pos3;
        public float Range3;
    }
}
