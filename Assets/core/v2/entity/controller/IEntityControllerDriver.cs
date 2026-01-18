using UnityEngine;

namespace StarfireV2
{
    public interface IEntityControllerDriver
    {
        int Priority { get; }
        bool IsActive { get; }

        bool IsWorldSpaceAim { get; }

        Vector2 GetMovementDirection();

        float GetThrottle();

        Vector2 GetDesiredAcceleration();

        float GetRotationInput();
        Vector2 GetAimDirection();

        bool IsFirePressed();
        bool IsWarpPressed();
        bool IsHyperdrivePressed();
    }
}