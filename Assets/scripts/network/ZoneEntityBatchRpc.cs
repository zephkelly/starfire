using Unity.Mathematics;
using Unity.NetCode;

namespace Starfire.Network
{
    public struct ZoneEntityBatchRpc : IRpcCommand
    {
        public int SourceNetworkId;
        public int BatchSequence;
        public int Count;

        public int Id0;
        public double2 Pos0;
        public float2 Vel0;

        public int Id1;
        public double2 Pos1;
        public float2 Vel1;

        public int Id2;
        public double2 Pos2;
        public float2 Vel2;

        public int Id3;
        public double2 Pos3;
        public float2 Vel3;

        public int Id4;
        public double2 Pos4;
        public float2 Vel4;

        public int Id5;
        public double2 Pos5;
        public float2 Vel5;

        public int Id6;
        public double2 Pos6;
        public float2 Vel6;

        public int Id7;
        public double2 Pos7;
        public float2 Vel7;

        public void Set(int slot, int id, double2 pos, float2 vel)
        {
            switch (slot)
            {
                case 0: Id0 = id; Pos0 = pos; Vel0 = vel; break;
                case 1: Id1 = id; Pos1 = pos; Vel1 = vel; break;
                case 2: Id2 = id; Pos2 = pos; Vel2 = vel; break;
                case 3: Id3 = id; Pos3 = pos; Vel3 = vel; break;
                case 4: Id4 = id; Pos4 = pos; Vel4 = vel; break;
                case 5: Id5 = id; Pos5 = pos; Vel5 = vel; break;
                case 6: Id6 = id; Pos6 = pos; Vel6 = vel; break;
                case 7: Id7 = id; Pos7 = pos; Vel7 = vel; break;
            }
        }

        public void Get(int slot, out int id, out double2 pos, out float2 vel)
        {
            switch (slot)
            {
                case 0: id = Id0; pos = Pos0; vel = Vel0; return;
                case 1: id = Id1; pos = Pos1; vel = Vel1; return;
                case 2: id = Id2; pos = Pos2; vel = Vel2; return;
                case 3: id = Id3; pos = Pos3; vel = Vel3; return;
                case 4: id = Id4; pos = Pos4; vel = Vel4; return;
                case 5: id = Id5; pos = Pos5; vel = Vel5; return;
                case 6: id = Id6; pos = Pos6; vel = Vel6; return;
                case 7: id = Id7; pos = Pos7; vel = Vel7; return;
                default: id = 0; pos = default; vel = default; return;
            }
        }
    }
}
