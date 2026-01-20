using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Runtime state for a single thruster instance.
    /// Tracks current thrust levels, world-space positions, and cached efficiency data.
    /// </summary>
    public class ThrusterState
    {
        /// <summary>
        /// The configuration definition for this thruster.
        /// </summary>
        public ThrusterDefinition Definition { get; }

        /// <summary>
        /// Optional marker component from the ship prefab.
        /// </summary>
        public ThrusterMarker Marker { get; set; }

        /// <summary>
        /// Optional visual effect controller.
        /// </summary>
        public ThrusterVisual Visual { get; set; }

        /// <summary>
        /// Current thrust output (0 to maxThrust).
        /// </summary>
        public float CurrentThrust { get; set; }

        /// <summary>
        /// Target thrust level we're ramping toward.
        /// </summary>
        public float TargetThrust { get; set; }

        /// <summary>
        /// Precomputed torque efficiency (torque per Newton of thrust).
        /// Positive = contributes to counter-clockwise rotation.
        /// Negative = contributes to clockwise rotation.
        /// </summary>
        public float TorqueEfficiency { get; set; }

        /// <summary>
        /// Current world-space position of the thruster.
        /// Updated each physics frame.
        /// </summary>
        public Vector2 WorldPosition { get; set; }

        /// <summary>
        /// Current world-space thrust direction.
        /// Updated each physics frame.
        /// </summary>
        public Vector2 WorldThrustDirection { get; set; }

        /// <summary>
        /// Whether this thruster can contribute to clockwise rotation.
        /// </summary>
        public bool CanContributeClockwise => TorqueEfficiency < -0.001f;

        /// <summary>
        /// Whether this thruster can contribute to counter-clockwise rotation.
        /// </summary>
        public bool CanContributeCounterClockwise => TorqueEfficiency > 0.001f;

        /// <summary>
        /// Absolute torque contribution per Newton (always positive).
        /// </summary>
        public float AbsoluteTorqueEfficiency => Mathf.Abs(TorqueEfficiency);

        /// <summary>
        /// Maximum torque this thruster can produce.
        /// </summary>
        public float MaxTorqueCapacity => AbsoluteTorqueEfficiency * Definition.maxThrust;

        /// <summary>
        /// Current normalized thrust (0 to 1).
        /// </summary>
        public float NormalizedThrust => Definition.maxThrust > 0f
            ? CurrentThrust / Definition.maxThrust
            : 0f;

        public ThrusterState(ThrusterDefinition definition)
        {
            Definition = definition;
            CurrentThrust = 0f;
            TargetThrust = 0f;
        }

        /// <summary>
        /// Calculate torque efficiency based on center of mass offset.
        /// Call this during initialization or when CoM changes.
        /// </summary>
        public void RecalculateEfficiency(Vector2 centerOfMassOffset = default)
        {
            TorqueEfficiency = Definition.CalculateTorqueEfficiency(centerOfMassOffset);
        }

        /// <summary>
        /// Update world-space position and direction based on ship transform.
        /// </summary>
        public void UpdateWorldSpace(Transform shipTransform)
        {
            if (Marker != null)
            {
                WorldPosition = Marker.MountPoint.position;
                WorldThrustDirection = Marker.WorldThrustDirection;
            }
            else
            {
                WorldPosition = shipTransform.TransformPoint(Definition.localPosition);
                WorldThrustDirection = shipTransform.TransformDirection(Definition.NormalizedThrustDirection);
            }
        }

        /// <summary>
        /// Smoothly update current thrust toward target thrust.
        /// </summary>
        public void UpdateThrust(float deltaTime)
        {
            if (Definition.responseTime <= 0f)
            {
                CurrentThrust = TargetThrust;
            }
            else
            {
                float maxDelta = (Definition.maxThrust / Definition.responseTime) * deltaTime;
                CurrentThrust = Mathf.MoveTowards(CurrentThrust, TargetThrust, maxDelta);
            }
        }

        /// <summary>
        /// Reset thrust state to zero.
        /// </summary>
        public void Reset()
        {
            CurrentThrust = 0f;
            TargetThrust = 0f;
        }
    }
}
