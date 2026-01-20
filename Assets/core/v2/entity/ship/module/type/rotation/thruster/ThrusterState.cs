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
        /// Base max thrust from definition (before damage).
        /// </summary>
        public float BaseMaxThrust { get; private set; }

        /// <summary>
        /// Efficiency multiplier (0-1). Damaged thrusters have lower efficiency.
        /// </summary>
        public float Efficiency { get; set; } = 1f;

        /// <summary>
        /// Effective max thrust after applying efficiency/damage.
        /// </summary>
        public float EffectiveMaxThrust => BaseMaxThrust * Efficiency;

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
        /// Maximum torque this thruster can produce (accounting for damage).
        /// </summary>
        public float MaxTorqueCapacity => AbsoluteTorqueEfficiency * EffectiveMaxThrust;

        /// <summary>
        /// Current normalized thrust (0 to 1).
        /// </summary>
        public float NormalizedThrust => EffectiveMaxThrust > 0f
            ? CurrentThrust / EffectiveMaxThrust
            : 0f;

        public ThrusterState(ThrusterDefinition definition)
        {
            Definition = definition;
            BaseMaxThrust = definition.maxThrust;
            CurrentThrust = 0f;
            TargetThrust = 0f;
        }

        /// <summary>
        /// Apply damage to this thruster (reduces efficiency).
        /// </summary>
        /// <param name="damagePercent">Amount to reduce efficiency (0-1)</param>
        public void ApplyDamage(float damagePercent)
        {
            Efficiency = Mathf.Clamp01(Efficiency - damagePercent);
        }

        /// <summary>
        /// Repair this thruster (restores efficiency).
        /// </summary>
        /// <param name="repairPercent">Amount to restore efficiency (0-1)</param>
        public void Repair(float repairPercent)
        {
            Efficiency = Mathf.Clamp01(Efficiency + repairPercent);
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
