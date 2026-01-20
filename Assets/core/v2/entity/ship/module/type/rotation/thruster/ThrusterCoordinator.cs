using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Coordinates multiple thrusters to achieve desired rotation torque.
    /// Distributes torque demand across available thrusters based on their efficiency.
    /// </summary>
    public class ThrusterCoordinator
    {
        private ThrusterState[] _thrusters;
        private Transform _shipTransform;
        private Rigidbody2D _rigidbody;

        // Cached capacity values
        private float _clockwiseCapacity;
        private float _counterClockwiseCapacity;

        /// <summary>
        /// Total torque capacity for clockwise rotation (negative torque).
        /// </summary>
        public float ClockwiseCapacity => _clockwiseCapacity;

        /// <summary>
        /// Total torque capacity for counter-clockwise rotation (positive torque).
        /// </summary>
        public float CounterClockwiseCapacity => _counterClockwiseCapacity;

        /// <summary>
        /// Total torque capacity in either direction.
        /// </summary>
        public float MaxTorqueCapacity => Mathf.Max(_clockwiseCapacity, _counterClockwiseCapacity);

        /// <summary>
        /// Number of thrusters being coordinated.
        /// </summary>
        public int ThrusterCount => _thrusters?.Length ?? 0;

        /// <summary>
        /// Initialize the coordinator with thruster states.
        /// </summary>
        public void Initialize(ThrusterState[] thrusters, Transform shipTransform, Rigidbody2D rigidbody)
        {
            _thrusters = thrusters;
            _shipTransform = shipTransform;
            _rigidbody = rigidbody;

            RecalculateEfficiency();
        }

        /// <summary>
        /// Recalculate torque efficiency for all thrusters.
        /// Call when center of mass changes or thrusters are modified.
        /// </summary>
        public void RecalculateEfficiency()
        {
            if (_thrusters == null) return;

            // Get center of mass offset in local space (usually zero for 2D)
            Vector2 comOffset = _rigidbody != null ? _rigidbody.centerOfMass : Vector2.zero;

            _clockwiseCapacity = 0f;
            _counterClockwiseCapacity = 0f;

            foreach (var thruster in _thrusters)
            {
                thruster.RecalculateEfficiency(comOffset);

                if (thruster.CanContributeClockwise)
                {
                    _clockwiseCapacity += thruster.MaxTorqueCapacity;
                }

                if (thruster.CanContributeCounterClockwise)
                {
                    _counterClockwiseCapacity += thruster.MaxTorqueCapacity;
                }
            }
        }

        /// <summary>
        /// Distribute desired torque across available thrusters.
        /// Sets TargetThrust on each thruster state.
        /// </summary>
        /// <param name="desiredTorque">Positive = counter-clockwise, negative = clockwise</param>
        /// <returns>True if the desired torque can be fully achieved</returns>
        public bool DistributeTorque(float desiredTorque)
        {
            if (_thrusters == null || _thrusters.Length == 0)
                return false;

            float absTorque = Mathf.Abs(desiredTorque);

            // Determine which direction we need
            bool needCounterClockwise = desiredTorque > 0f;

            // Get available capacity for this direction
            float availableCapacity = needCounterClockwise
                ? _counterClockwiseCapacity
                : _clockwiseCapacity;

            // Calculate scale factor (how much of max capacity to use)
            float scale = availableCapacity > 0.001f
                ? Mathf.Clamp01(absTorque / availableCapacity)
                : 0f;

            // Distribute to thrusters
            foreach (var thruster in _thrusters)
            {
                bool canContribute = needCounterClockwise
                    ? thruster.CanContributeCounterClockwise
                    : thruster.CanContributeClockwise;

                if (canContribute && thruster.AbsoluteTorqueEfficiency > 0.001f)
                {
                    // Set target thrust proportional to this thruster's contribution
                    thruster.TargetThrust = thruster.Definition.maxThrust * scale;
                }
                else
                {
                    // This thruster doesn't contribute to the desired direction
                    thruster.TargetThrust = 0f;
                }
            }

            return absTorque <= availableCapacity;
        }

        /// <summary>
        /// Update thruster positions/directions and smooth thrust transitions.
        /// Call every physics frame before ApplyForces.
        /// </summary>
        public void UpdateThrusters(float deltaTime)
        {
            if (_thrusters == null || _shipTransform == null) return;

            foreach (var thruster in _thrusters)
            {
                thruster.UpdateWorldSpace(_shipTransform);
                thruster.UpdateThrust(deltaTime);
            }
        }

        /// <summary>
        /// Apply forces from all active thrusters to the rigidbody.
        /// </summary>
        public void ApplyForces()
        {
            if (_thrusters == null || _rigidbody == null) return;

            foreach (var thruster in _thrusters)
            {
                if (thruster.CurrentThrust > 0.001f)
                {
                    Vector2 force = thruster.WorldThrustDirection * thruster.CurrentThrust;
                    _rigidbody.AddForceAtPosition(force, thruster.WorldPosition, ForceMode2D.Force);
                }
            }
        }

        /// <summary>
        /// Update all thruster visuals based on current thrust levels.
        /// </summary>
        public void UpdateVisuals()
        {
            if (_thrusters == null) return;

            foreach (var thruster in _thrusters)
            {
                if (thruster.Visual != null)
                {
                    thruster.Visual.SetIntensity(thruster.NormalizedThrust);
                }
            }
        }

        /// <summary>
        /// Reset all thrusters to zero thrust.
        /// </summary>
        public void ResetAll()
        {
            if (_thrusters == null) return;

            foreach (var thruster in _thrusters)
            {
                thruster.Reset();
            }
        }

        /// <summary>
        /// Get the total torque currently being produced.
        /// Useful for debugging and UI feedback.
        /// </summary>
        public float GetCurrentTorque()
        {
            if (_thrusters == null) return 0f;

            float totalTorque = 0f;
            foreach (var thruster in _thrusters)
            {
                totalTorque += thruster.CurrentThrust * thruster.TorqueEfficiency;
            }
            return totalTorque;
        }

        /// <summary>
        /// Get readonly access to thruster states for debugging/UI.
        /// </summary>
        public ThrusterState[] GetThrusters() => _thrusters;
    }
}
