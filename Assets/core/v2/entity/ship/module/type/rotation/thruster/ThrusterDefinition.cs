using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Serializable definition of a single rotational thruster.
    /// Configured in ShipRotationModuleConfig and matched to ThrusterMarkers on prefabs.
    /// </summary>
    [System.Serializable]
    public class ThrusterDefinition
    {
        [Tooltip("Unique identifier matching ThrusterMarker.SlotId on prefab (optional)")]
        public string slotId;

        [Tooltip("Local position relative to ship center of mass")]
        public Vector2 localPosition;

        [Tooltip("Thrust direction in local space (will be normalized)")]
        public Vector2 thrustDirection = Vector2.up;

        [Tooltip("Maximum thrust force in Newtons")]
        [Min(0f)]
        public float maxThrust = 100f;

        [Tooltip("Time to ramp thrust up/down (0 = instant response)")]
        [Range(0f, 0.5f)]
        public float responseTime = 0.05f;

        /// <summary>
        /// Gets the normalized thrust direction.
        /// </summary>
        public Vector2 NormalizedThrustDirection => thrustDirection.normalized;

        /// <summary>
        /// Calculates the torque efficiency of this thruster.
        /// Positive efficiency = counter-clockwise contribution.
        /// Negative efficiency = clockwise contribution.
        /// Magnitude = torque per Newton of thrust.
        /// </summary>
        /// <param name="centerOfMass">Center of mass in local space (usually Vector2.zero)</param>
        /// <returns>Torque efficiency (torque per Newton)</returns>
        public float CalculateTorqueEfficiency(Vector2 centerOfMass = default)
        {
            // r = position relative to center of mass
            Vector2 r = localPosition - centerOfMass;

            // d = normalized thrust direction
            Vector2 d = NormalizedThrustDirection;

            // 2D cross product: torque = r.x * d.y - r.y * d.x
            // Positive = counter-clockwise torque
            // Negative = clockwise torque
            return r.x * d.y - r.y * d.x;
        }
    }
}
