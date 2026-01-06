using UnityEngine;

namespace Starfire.Entity
{
    public interface IControllerDriver
    {
        int Priority { get; }
        bool IsActive { get; }

        Vector2 GetMovementDirection();
        float GetRotationInput();
        Vector2 GetAimDirection();

        bool IsFirePressed();
        bool IsWarpPressed();
        bool IsHyperdrivePressed();
    }
}