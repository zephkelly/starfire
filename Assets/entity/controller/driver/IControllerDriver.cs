using UnityEngine;

namespace Starfire.Entity
{
    public interface IControllerDriver
    {
        int Priority { get; }
        bool IsActive { get; }

        /// <summary>
        /// If true, GetAimDirection returns world coordinates.
        /// If false, GetAimDirection returns screen coordinates (e.g., mouse position).
        /// </summary>
        bool IsWorldSpaceAim { get; }

        Vector2 GetMovementDirection();

        /// <summary>
        /// Returns the throttle value (0-1) for movement speed scaling.
        /// 0 = no movement, 1 = full speed.
        /// </summary>
        float GetThrottle();

        /// <summary>
        /// Returns desired acceleration vector for physics-based steering.
        /// Returns Vector2.zero if not using acceleration mode (falls back to direction/throttle).
        /// </summary>
        Vector2 GetDesiredAcceleration();

        float GetRotationInput();
        Vector2 GetAimDirection();

        bool IsFirePressed();
        bool IsWarpPressed();
        bool IsHyperdrivePressed();
    }
}