using UnityEngine;

namespace Starfire.Core.Cam
{
    public enum CameraTargetType
    {
        Transform,
        PhysicsBody
    }

    public interface ICameraTarget
    {
        Vector2 Position { get; }
        Vector2 InterpolatedPosition { get; }
        Vector2 Velocity { get; }
        Vector2 FocusDirection { get; }
        Vector2 FocusDirectionRaw { get; }
        bool HasFocus { get; }
        bool IsWorldSpaceAim { get; }
        float AimMaxRadius { get; }
        bool IsValid { get; }
        float Priority { get; }
        CameraTargetType TargetType { get; }
    }
}
