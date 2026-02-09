using Unity.Mathematics;
using Unity.Entities;

namespace Starfire.Core
{
    public struct ControlInput : IComponentData
    {
        public byte DriverType; // 0 = player, 1 = AI, 2 = remote player (multiplayer)

        public float Throttle;
        public float2 MovementDirection;
        public float2 AimDirection;
        public byte FirePressed;
        public byte WarpPressed;
    }
}