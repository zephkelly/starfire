using System;
using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    [Serializable]
    public class TurretSettings
    {
        [Tooltip("If true, weapon can rotate to track targets")]
        public bool isTurret = false;

        [Tooltip("Rotation speed in degrees per second")]
        public float rotationSpeed = 180f;

        [Tooltip("Maximum firing arc in degrees (360 = full rotation)")]
        [Range(0f, 360f)]
        public float firingArc = 360f;

        [Tooltip("If true, weapon can fire while rotating to target")]
        public bool canFireWhileRotating = false;

        [Tooltip("Angle tolerance for firing (weapon fires when within this angle of target)")]
        public float firingTolerance = 5f;
    }
}
