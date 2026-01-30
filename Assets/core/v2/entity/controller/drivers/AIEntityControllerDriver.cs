using System;
using UnityEngine;

namespace StarfireV2
{
    [Serializable]
    public class AIEntityControllerDriver : IEntityControllerDriver
    {
        [field: SerializeField]
        public int Priority { get; private set; } = 5;
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
        /// Whether the BT has explicitly set an aim target.
        /// When false, rotation is not applied (ship maintains heading).
        /// </summary>
        public bool HasAimTarget { get; set; }

        /// <summary>
        /// World position to aim at. Set by behavior system.
        /// Setting this also marks HasAimTarget as true.
        /// </summary>
        public Vector2 AimPosition
        {
            get => _aimPosition;
            set
            {
                _aimPosition = value;
                HasAimTarget = true;
            }
        }
        private Vector2 _aimPosition;

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

        // Parameterless constructor for serialization
        public AIEntityControllerDriver() { }

        public AIEntityControllerDriver(int priority)
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
