using Unity.Mathematics;

namespace Starfire.Core
{
    /// <summary>
    /// Input packet that drives ship behavior.
    /// Produced by IBehaviorController, consumed by ShipInstance.
    /// </summary>
    public struct ControlInput
    {
        public byte DriverType; // 0 = player, 1 = AI, 2 = remote player (multiplayer)

        public float Throttle;
        public float2 MovementDirection;
        public float2 AimDirection;
        public byte FirePressed;
        public byte WarpPressed;
    }
}