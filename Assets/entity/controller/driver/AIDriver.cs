using UnityEngine;

namespace Starfire.Entity
{
    /// <summary>
    /// AI implementation of IControllerDriver.
    /// Behavior systems (e.g., PatrolBehavior) set the driver state,
    /// and the controller queries it each frame.
    /// </summary>
    public class AIDriver : IControllerDriver
    {
        public int Priority { get; }
        public bool IsActive { get; set; } = true;
        public bool IsWorldSpaceAim => true;

        /// <summary>
        /// Normalized movement direction. Set by behavior system.
        /// </summary>
        public Vector2 MovementDirection { get; set; }

        /// <summary>
        /// Throttle value (0-1) for movement speed scaling.
        /// 0 = no movement, 1 = full speed. Set by behavior system.
        /// </summary>
        public float Throttle { get; set; } = 1f;

        /// <summary>
        /// World position to aim at. Set by behavior system.
        /// </summary>
        public Vector2 AimPosition { get; set; }

        /// <summary>
        /// Desired acceleration for physics-based steering. Set by behavior system.
        /// When non-zero, overrides MovementDirection/Throttle in ProcessMovement.
        /// </summary>
        public Vector2 DesiredAcceleration { get; set; } = Vector2.zero;

        /// <summary>
        /// Whether fire should be triggered this frame. Set by behavior system.
        /// </summary>
        public bool FireRequested { get; set; }

        /// <summary>
        /// Whether warp should be engaged. Set by behavior system.
        /// </summary>
        public bool WarpRequested { get; set; }

        /// <summary>
        /// Whether hyperdrive should be engaged. Set by behavior system.
        /// </summary>
        public bool HyperdriveRequested { get; set; }

        public AIDriver(int priority = 5)
        {
            Priority = priority;
        }

        public Vector2 GetMovementDirection() => MovementDirection;
        public float GetThrottle() => Throttle;
        public Vector2 GetDesiredAcceleration() => DesiredAcceleration;
        public float GetRotationInput() => 0f;
        public Vector2 GetAimDirection() => AimPosition;
        public bool IsFirePressed() => FireRequested;
        public bool IsWarpPressed() => WarpRequested;
        public bool IsHyperdrivePressed() => HyperdriveRequested;
    }
}
