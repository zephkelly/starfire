using Unity.Mathematics;

namespace Starfire.Core
{
    public interface IController
    {
        float Throttle { get; }
        float2 MovementDirection { get; }
        float2 AimDirection { get; }
        bool FirePressed { get; }
        bool WarpPressed { get; }
    }
}
