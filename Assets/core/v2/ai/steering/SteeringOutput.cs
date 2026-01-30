using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Output from a steering behavior calculation.
    /// </summary>
    public struct SteeringOutput
    {
        /// <summary>
        /// The velocity the agent wants to achieve.
        /// </summary>
        public Vector2 DesiredVelocity;

        /// <summary>
        /// The force to apply to reach desired velocity (desiredVelocity - currentVelocity).
        /// </summary>
        public Vector2 SteeringForce;

        /// <summary>
        /// True if the agent is in the braking/slowing zone.
        /// </summary>
        public bool ShouldBrake;

        /// <summary>
        /// Distance to the target position.
        /// </summary>
        public float DistanceToTarget;
    }
}
