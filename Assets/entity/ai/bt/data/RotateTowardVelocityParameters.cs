using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for RotateTowardVelocityAction.
    /// Rotates entity to face its movement direction.
    /// </summary>
    [Serializable]
    public class RotateTowardVelocityParameters : IBTNodeParameters
    {
        /// <summary>
        /// Minimum speed required to rotate. Below this, rotation is skipped.
        /// </summary>
        public float minSpeedThreshold = 0.5f;
    }
}
