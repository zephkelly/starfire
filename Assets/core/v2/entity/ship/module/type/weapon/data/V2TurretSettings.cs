using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for weapon turret behavior.
    /// </summary>
    [Serializable]
    public class V2TurretSettings
    {
        [Tooltip("Whether this weapon can rotate to track targets.")]
        public bool isTurret = false;

        [Tooltip("Rotation speed in degrees per second.")]
        public float rotationSpeed = 180f;

        [Tooltip("Maximum rotation arc in degrees (0-360). 0 = fixed forward, 360 = full rotation.")]
        [Range(0f, 360f)]
        public float firingArc = 360f;

        [Tooltip("Whether the weapon can fire while still rotating to target.")]
        public bool canFireWhileRotating = false;

        [Tooltip("Angle tolerance in degrees for considering the weapon aimed at target.")]
        public float firingTolerance = 5f;

        [Tooltip("Whether to predict target position based on velocity for leading shots.")]
        public bool predictTargetPosition = false;

        /// <summary>
        /// Creates default turret settings for a fixed (non-rotating) weapon.
        /// </summary>
        public static V2TurretSettings Fixed => new V2TurretSettings
        {
            isTurret = false
        };

        /// <summary>
        /// Creates default turret settings for a fully rotating turret.
        /// </summary>
        public static V2TurretSettings FullRotation => new V2TurretSettings
        {
            isTurret = true,
            rotationSpeed = 180f,
            firingArc = 360f,
            canFireWhileRotating = false,
            firingTolerance = 5f
        };
    }
}
