using Unity.Mathematics;
using Unity.NetCode;

namespace Starfire.Network
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PlayerInput : IInputComponentData
    {
        public float Throttle;
        public float2 MovementDirection;
        public float2 AimDirection;
        public InputEvent Fire;
        public InputEvent Warp;
    }
}
