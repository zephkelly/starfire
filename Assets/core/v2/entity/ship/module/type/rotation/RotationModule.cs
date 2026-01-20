using Starfire.Entity.Modules.Rotation;
using UnityEngine;

namespace StarfireV2
{
    public enum ThrusterControlState
    {
        Idle,       // Within deadzone AND near-zero velocity - all thrusters off
        Accelerate, // Need to rotate toward target (proportional thrust)
        Coast,      // Momentum sufficient, no thrust needed
        Brake,      // Slowing down (proportional thrust)
        Settling    // Close to target, PD controller micro-corrections
    }

    public class RotationModule : IShipRotationModule
    {
        private readonly ShipRotationModuleConfig _config;
        private IEntityController _controller;

        // Smooth mode state
        private float _smoothDampVelocity;

        // Physics mode state
        private float _previousError;
        private float _originalAngularDrag;

        // Thruster-based mode state
        private ThrusterCoordinator _thrusterCoordinator;
        private ThrusterState[] _thrusterStates;
        private ThrusterControlState _currentState = ThrusterControlState.Idle;
        private float _cachedMaxAngularAcceleration;
        private bool _accelerationCached;

        public ShipModuleCategory Category => ShipModuleCategory.Propulsion;
        public ShipModuleType Type => ShipModuleType.RotationalThrusters;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public bool IsEnabled { get; set; } = true;

        public float RotationSpeed => _config.RotationSpeed;

        public RotationModule(ShipRotationModuleConfig config)
        {
            _config = config;
        }

        public void OnAttach(IEntityController controller)
        {
            _controller = controller;

            if (_config.RotationMode == RotationMode.Physics && _controller?.Rigid2D != null)
            {
                _originalAngularDrag = _controller.Rigid2D.angularDamping;
                _controller.Rigid2D.angularDamping = _config.AngularDrag;
            }

            if (_config.RotationMode == RotationMode.ThrusterBased)
            {
                InitializeThrusters();
            }
        }

        public void OnDetach()
        {
            if (_config.RotationMode == RotationMode.Physics && _controller?.Rigid2D != null)
            {
                _controller.Rigid2D.angularDamping = _originalAngularDrag;
            }

            if (_config.RotationMode == RotationMode.ThrusterBased)
            {
                CleanupThrusters();
            }

            _controller = null;
            ResetState();
        }

        public void OnUpdate(float deltaTime)
        {
            // Rotation is processed via ProcessRotation, not OnUpdate
        }

        public void ProcessRotation(RotationInputData input, float deltaTime)
        {
            if (!IsEnabled || _controller?.Rigid2D == null) return;

            switch (_config.RotationMode)
            {
                case RotationMode.Instant:
                    ProcessInstantRotation(input, deltaTime);
                    break;

                case RotationMode.Smooth:
                    ProcessSmoothRotation(input, deltaTime);
                    break;

                case RotationMode.Physics:
                    ProcessPhysicsRotation(input, deltaTime);
                    break;

                case RotationMode.ThrusterBased:
                    ProcessThrusterRotation(input, deltaTime);
                    break;
            }
        }

        private void ProcessInstantRotation(RotationInputData input, float deltaTime)
        {
            float targetAngle = input.GetTargetAngle(_config.SpriteOffset);

            if (_config.RespectMaxSpeed)
            {
                float maxDelta = RotationSpeed * deltaTime;
                float angleDelta = Mathf.DeltaAngle(input.CurrentRotation, targetAngle);
                angleDelta = Mathf.Clamp(angleDelta, -maxDelta, maxDelta);
                targetAngle = input.CurrentRotation + angleDelta;
            }

            _controller.Rigid2D.MoveRotation(targetAngle);
        }

        private void ProcessSmoothRotation(RotationInputData input, float deltaTime)
        {
            float targetAngle = input.GetTargetAngle(_config.SpriteOffset);

            float angleDelta = Mathf.Abs(Mathf.DeltaAngle(input.CurrentRotation, targetAngle));
            if (angleDelta < _config.SmoothDeadzone) return;

            float newAngle;

            if (_config.UseSmoothDamp)
            {
                newAngle = Mathf.SmoothDampAngle(
                    input.CurrentRotation,
                    targetAngle,
                    ref _smoothDampVelocity,
                    1f / _config.SmoothingFactor,
                    RotationSpeed,
                    deltaTime
                );
            }
            else
            {
                float t = _config.SmoothingFactor * deltaTime;
                newAngle = Mathf.LerpAngle(input.CurrentRotation, targetAngle, t);
            }

            _controller.Rigid2D.MoveRotation(newAngle);
        }

        private void ProcessPhysicsRotation(RotationInputData input, float deltaTime)
        {
            float error = input.GetAngleDelta(_config.SpriteOffset);
            float currentVel = _controller.Rigid2D.angularVelocity;

            // Deadzone - skip applying torque, let angular drag slow naturally
            if (Mathf.Abs(error) < _config.PhysicsDeadzone)
            {
                _previousError = error;
                ClampAngularVelocity();
                return;
            }

            float torque = CalculatePIDTorque(error, deltaTime);
            torque = Mathf.Clamp(torque, -_config.MaxTorque, _config.MaxTorque);

            // Don't apply torque that would increase velocity beyond max
            bool wouldIncreaseVelocity = (torque > 0 && currentVel > 0) || (torque < 0 && currentVel < 0);
            if (wouldIncreaseVelocity && Mathf.Abs(currentVel) >= _config.MaxAngularVelocity)
            {
                torque = 0f;
            }

            _controller.Rigid2D.AddTorque(torque, ForceMode2D.Force);

            // Hard clamp angular velocity
            ClampAngularVelocity();
        }

        private void ClampAngularVelocity()
        {
            float currentVel = _controller.Rigid2D.angularVelocity;
            if (Mathf.Abs(currentVel) > _config.MaxAngularVelocity)
            {
                _controller.Rigid2D.angularVelocity = Mathf.Sign(currentVel) * _config.MaxAngularVelocity;
            }
        }

        /// <summary>
        /// Calculate maximum angular acceleration based on thruster capacity and ship inertia.
        /// Formula: alpha = tau / I (angular acceleration = torque / moment of inertia)
        /// </summary>
        private float CalculateMaxAngularAcceleration(bool forBraking, float error)
        {
            if (_accelerationCached && !forBraking)
                return _cachedMaxAngularAcceleration;

            float maxTorque;
            if (_thrusterCoordinator != null)
            {
                // Use actual thruster capacity for the required direction
                // When braking, we use the OPPOSITE direction's capacity
                if (forBraking)
                {
                    // If error > 0, we were rotating CCW, so brake with CW thrusters
                    maxTorque = error > 0
                        ? _thrusterCoordinator.ClockwiseCapacity
                        : _thrusterCoordinator.CounterClockwiseCapacity;
                }
                else
                {
                    maxTorque = error > 0
                        ? _thrusterCoordinator.CounterClockwiseCapacity
                        : _thrusterCoordinator.ClockwiseCapacity;
                }
            }
            else
            {
                maxTorque = _config.MaxTorque;
            }

            // Get moment of inertia from Rigidbody2D
            float inertia = _controller?.Rigid2D?.inertia ?? 1f;
            if (inertia <= 0.001f)
                inertia = 1f; // Fallback for auto-calculated inertia of 0

            // alpha = tau / I, converted to deg/s^2
            float angularAcceleration = (maxTorque / inertia) * Mathf.Rad2Deg;

            if (!forBraking)
            {
                _cachedMaxAngularAcceleration = angularAcceleration;
                _accelerationCached = true;
            }

            return angularAcceleration;
        }

        /// <summary>
        /// Invalidate cached acceleration (call when thrusters change or mass changes)
        /// </summary>
        public void InvalidateAccelerationCache()
        {
            _accelerationCached = false;
        }

        /// <summary>
        /// Determine the thruster control state based on physics.
        /// State priority: IDLE → HIGH VELOCITY (coast/brake) → SETTLING → ACCELERATE
        /// </summary>
        private ThrusterControlState DetermineControlState(float error, float angularVelocity)
        {
            float absError = Mathf.Abs(error);
            float absVelocity = Mathf.Abs(angularVelocity);

            // 1. IDLE: Both error AND velocity are tiny - all thrusters off
            if (absError < _config.PhysicsDeadzone && absVelocity < _config.VelocityDeadzone)
            {
                return ThrusterControlState.Idle;
            }

            // 2. HIGH VELOCITY: Must handle braking BEFORE settling
            // Only enter settling when velocity is low enough for PD controller to handle
            if (absVelocity > _config.SettlingVelocityThreshold)
            {
                return DetermineHighVelocityState(error, angularVelocity, absError, absVelocity);
            }

            // 3. LOW VELOCITY + CLOSE TO TARGET: Safe to use settling PD controller
            if (absError < _config.SettlingAngleThreshold)
            {
                return ThrusterControlState.Settling;
            }

            // 4. LOW VELOCITY + FAR FROM TARGET: Need to accelerate
            return ThrusterControlState.Accelerate;
        }

        /// <summary>
        /// Determine state when velocity is above settling threshold.
        /// Uses stopping distance calculation for coast/brake decisions.
        /// </summary>
        private ThrusterControlState DetermineHighVelocityState(float error, float angularVelocity, float absError, float absVelocity)
        {
            // Check if rotating toward target
            // error > 0 means target is CCW from current (need CCW rotation = positive angular velocity)
            // error < 0 means target is CW from current (need CW rotation = negative angular velocity)
            bool rotatingTowardTarget = (error > 0 && angularVelocity > 0) ||
                                        (error < 0 && angularVelocity < 0);

            // If not rotating toward target, brake first
            if (!rotatingTowardTarget)
            {
                return ThrusterControlState.Brake;
            }

            // Rotating toward target - check if we need to start braking based on stopping distance
            float maxBrakingAcceleration = CalculateMaxAngularAcceleration(forBraking: true, error);

            // Avoid division by zero
            if (maxBrakingAcceleration < 0.001f)
            {
                return ThrusterControlState.Accelerate;
            }

            // Calculate stopping angle using physics formula: theta = omega^2 / (2 * alpha)
            float stoppingAngle = (absVelocity * absVelocity) / (2f * maxBrakingAcceleration);
            stoppingAngle *= _config.BrakingSafetyMargin; // Apply safety margin

            // Apply hysteresis to prevent state chatter
            float threshold = stoppingAngle;
            if (_currentState == ThrusterControlState.Coast)
            {
                threshold += _config.StateTransitionHysteresis;
            }
            else if (_currentState == ThrusterControlState.Brake)
            {
                threshold -= _config.StateTransitionHysteresis;
            }

            // Determine brake vs accelerate vs coast
            // Priority: Brake if at stopping distance, then accelerate if not at max velocity, else coast
            if (absError <= threshold)
            {
                return ThrusterControlState.Brake;
            }

            // Keep accelerating until near max angular velocity
            // Only coast when at max velocity (waiting to reach braking point)
            if (absVelocity < _config.MaxAngularVelocity * 0.95f)
            {
                return ThrusterControlState.Accelerate;
            }

            // At max velocity - coast until we need to brake
            return ThrusterControlState.Coast;
        }

        /// <summary>
        /// Calculate torque to accelerate toward target.
        /// Uses proportional control based on angular error.
        /// </summary>
        private float CalculateAccelerationTorque(float error, float angularVelocity)
        {
            // Determine desired direction
            float direction = Mathf.Sign(error); // +1 for CCW, -1 for CW
            float absError = Mathf.Abs(error);
            float absVelocity = Mathf.Abs(angularVelocity);

            // Check if we're already at max angular velocity going the right direction
            if (absVelocity >= _config.MaxAngularVelocity)
            {
                if (Mathf.Sign(angularVelocity) == direction)
                {
                    return 0f; // Already at max speed in right direction
                }
            }

            // Get max torque capacity for this direction
            float maxTorque = direction > 0
                ? _thrusterCoordinator.CounterClockwiseCapacity
                : _thrusterCoordinator.ClockwiseCapacity;

            // FIX: Proportional acceleration based on error
            // Scale thrust: larger error = more thrust, small error = less thrust
            float accelFactor = Mathf.Clamp01(absError / _config.ReferenceAngle);

            // Apply minimum thrust fraction to prevent thruster stutter at low values
            if (accelFactor > 0f && accelFactor < _config.MinimumThrustFraction)
            {
                accelFactor = _config.MinimumThrustFraction;
            }

            // Also reduce thrust as we approach max velocity to prevent overshoot
            float velocityHeadroom = 1f - Mathf.Clamp01(absVelocity / _config.MaxAngularVelocity);
            accelFactor *= Mathf.Lerp(0.2f, 1f, velocityHeadroom);

            return direction * maxTorque * accelFactor;
        }

        /// <summary>
        /// Calculate torque to brake (stop rotation).
        /// Uses proportional control based on angular velocity.
        /// </summary>
        private float CalculateBrakingTorque(float angularVelocity)
        {
            float absVelocity = Mathf.Abs(angularVelocity);

            // FIX: Use velocity deadzone instead of hardcoded 0.1f
            if (absVelocity < _config.VelocityDeadzone)
            {
                return 0f; // Already nearly stopped
            }

            // Brake opposite to velocity direction
            float direction = -Mathf.Sign(angularVelocity);

            float maxTorque = direction > 0
                ? _thrusterCoordinator.CounterClockwiseCapacity
                : _thrusterCoordinator.ClockwiseCapacity;

            // FIX: Proportional braking based on velocity
            // Higher velocity = more braking thrust, low velocity = gentle braking
            float brakingFactor = Mathf.Clamp01(absVelocity / _config.ReferenceVelocity);

            // Apply minimum thrust fraction to prevent thruster stutter
            if (brakingFactor > 0f && brakingFactor < _config.MinimumThrustFraction)
            {
                brakingFactor = _config.MinimumThrustFraction;
            }

            return direction * maxTorque * brakingFactor;
        }

        /// <summary>
        /// Calculate torque for settling phase using simplified proportional control.
        /// Prevents oscillation by using gentle corrections and coasting when appropriate.
        /// </summary>
        private float CalculateSettlingTorque(float error, float angularVelocity)
        {
            float absError = Mathf.Abs(error);
            float absVelocity = Mathf.Abs(angularVelocity);

            // If we're very close to settled, allow zero torque
            if (absError < _config.PhysicsDeadzone * 0.5f && absVelocity < _config.VelocityDeadzone * 0.5f)
            {
                return 0f;
            }

            // Check if moving toward target
            bool movingTowardTarget = (error > 0 && angularVelocity > 0) ||
                                       (error < 0 && angularVelocity < 0);

            // Estimate if current velocity will overshoot the target
            // Time to reach target at current velocity: t = error / velocity
            // If velocity would carry us past the target, we need to brake
            float timeToTarget = absVelocity > 0.1f ? absError / absVelocity : float.MaxValue;
            bool willOvershoot = movingTowardTarget && timeToTarget < 0.1f; // Will reach target in < 100ms

            float settlingFactor;

            if (movingTowardTarget && !willOvershoot)
            {
                // Moving toward target with good trajectory - COAST (no thrust)
                // Let momentum carry us, velocity will naturally decay
                settlingFactor = 0f;
            }
            else if (movingTowardTarget && willOvershoot)
            {
                // About to overshoot - apply gentle braking (opposite to velocity)
                float brakeFactor = Mathf.Clamp01(absVelocity / _config.SettlingVelocityThreshold);
                settlingFactor = -Mathf.Sign(angularVelocity) * brakeFactor * 0.5f; // Gentle brake
            }
            else
            {
                // Moving away from target or stopped - apply proportional correction toward target
                float normalizedError = Mathf.Clamp01(absError / _config.SettlingAngleThreshold);
                // Use only P term, scaled down for gentler correction
                settlingFactor = Mathf.Sign(error) * normalizedError * _config.SettlingProportionalGain * 0.5f;
                settlingFactor = Mathf.Clamp(settlingFactor, -1f, 1f);
            }

            // Get appropriate capacity based on direction
            float direction = Mathf.Sign(settlingFactor);
            float maxTorque = direction > 0
                ? _thrusterCoordinator.CounterClockwiseCapacity
                : _thrusterCoordinator.ClockwiseCapacity;

            // Convert normalized factor to actual torque
            float desiredTorque = settlingFactor * maxTorque;
            float absTorque = Mathf.Abs(desiredTorque);

            // Apply minimum thrust fraction if torque is non-zero but too small
            if (absTorque > 0.001f && absTorque < maxTorque * _config.MinimumThrustFraction)
            {
                return Mathf.Sign(desiredTorque) * maxTorque * _config.MinimumThrustFraction;
            }

            return desiredTorque;
        }

        private float CalculatePIDTorque(float error, float deltaTime)
        {
            float pTerm = error * _config.ProportionalGain;

            float derivative = deltaTime > 0f ? (error - _previousError) / deltaTime : 0f;
            float dTerm = derivative * _config.DerivativeGain;

            _previousError = error;

            return pTerm + dTerm;
        }

        private void ResetState()
        {
            _smoothDampVelocity = 0f;
            _previousError = 0f;
        }

        #region Thruster-Based Mode

        private void InitializeThrusters()
        {
            var definitions = _config.Thrusters;
            if (definitions == null || definitions.Length == 0)
            {
                Debug.LogWarning($"[RotationModule] ThrusterBased mode selected but no thrusters defined. Falling back to Physics mode behavior.");
                return;
            }

            Debug.Log($"[RotationModule] Initializing {definitions.Length} thrusters");

            // Create thruster states from definitions
            _thrusterStates = new ThrusterState[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                _thrusterStates[i] = new ThrusterState(definitions[i]);
                var def = definitions[i];
                float efficiency = def.CalculateTorqueEfficiency();
                Debug.Log($"[RotationModule] Thruster {i}: pos={def.localPosition}, dir={def.thrustDirection}, maxThrust={def.maxThrust}, efficiency={efficiency:F3}");
            }

            // Try to find markers on the ship if auto-discover is enabled
            if (_config.AutoDiscoverThrusters && _controller?.Transform != null)
            {
                var markers = _controller.Transform.GetComponentsInChildren<ThrusterMarker>();
                foreach (var marker in markers)
                {
                    // Match marker to thruster state by slotId
                    foreach (var state in _thrusterStates)
                    {
                        if (!string.IsNullOrEmpty(state.Definition.slotId) &&
                            state.Definition.slotId == marker.SlotId)
                        {
                            state.Marker = marker;
                            break;
                        }
                    }
                }
            }

            // Initialize visual effects if configured
            if (_config.ThrusterVisualConfig != null)
            {
                InitializeThrusterVisuals();
            }

            // Initialize coordinator
            _thrusterCoordinator = new ThrusterCoordinator();
            _thrusterCoordinator.Initialize(_thrusterStates, _controller.Transform, _controller.Rigid2D);

            Debug.Log($"[RotationModule] Coordinator initialized. CW capacity={_thrusterCoordinator.ClockwiseCapacity:F2}, CCW capacity={_thrusterCoordinator.CounterClockwiseCapacity:F2}");
        }

        private void InitializeThrusterVisuals()
        {
            var visualConfig = _config.ThrusterVisualConfig;
            if (visualConfig == null) return;

            foreach (var state in _thrusterStates)
            {
                // Determine spawn point
                Transform spawnParent = state.Marker?.MountPoint ?? _controller.Transform;
                Vector3 localPos = state.Marker != null ? Vector3.zero : (Vector3)state.Definition.localPosition;

                // Orient visual to face thrust direction
                Vector2 thrustDir = state.Marker?.ThrustDirection ?? state.Definition.NormalizedThrustDirection;
                float angle = Mathf.Atan2(thrustDir.y, thrustDir.x) * Mathf.Rad2Deg - 90f;

                ThrusterVisual visual;

                if (visualConfig.UseCodeGenerated)
                {
                    // Create visual entirely from code - no prefab needed
                    visual = ThrusterVisual.CreateFromCode(spawnParent, visualConfig);
                    visual.transform.localPosition = localPos;
                    visual.transform.localRotation = Quaternion.Euler(0, 0, angle);
                }
                else
                {
                    // Spawn visual prefab
                    var visualGO = Object.Instantiate(visualConfig.VisualPrefab, spawnParent);
                    visualGO.transform.localPosition = localPos;
                    visualGO.transform.localRotation = Quaternion.Euler(0, 0, angle);

                    // Get ThrusterVisual component from prefab
                    visual = visualGO.GetComponent<ThrusterVisual>();
                    if (visual != null)
                    {
                        visual.Initialize(visualConfig);
                    }
                }

                state.Visual = visual;
            }
        }

        private void CleanupThrusters()
        {
            if (_thrusterStates != null)
            {
                foreach (var state in _thrusterStates)
                {
                    if (state.Visual != null)
                    {
                        Object.Destroy(state.Visual.gameObject);
                        state.Visual = null;
                    }
                    state.Marker = null;
                }
            }

            _thrusterStates = null;
            _thrusterCoordinator = null;
        }

        private float _debugLogTimer;

        private void ProcessThrusterRotation(RotationInputData input, float deltaTime)
        {
            // Fallback if thrusters not initialized
            if (_thrusterCoordinator == null || _thrusterStates == null || _thrusterStates.Length == 0)
            {
                Debug.LogWarning("[RotationModule] ProcessThrusterRotation called but thrusters not initialized - falling back to Physics mode");
                ProcessPhysicsRotation(input, deltaTime);
                return;
            }

            float error = input.GetAngleDelta(_config.SpriteOffset);
            float angularVelocity = _controller.Rigid2D.angularVelocity;

            // Determine control state using physics-based state machine
            ThrusterControlState previousState = _currentState;
            _currentState = DetermineControlState(error, angularVelocity);

            // Calculate torque based on current state
            float desiredTorque = _currentState switch
            {
                ThrusterControlState.Idle => 0f,
                ThrusterControlState.Accelerate => CalculateAccelerationTorque(error, angularVelocity),
                ThrusterControlState.Coast => 0f,
                ThrusterControlState.Brake => CalculateBrakingTorque(angularVelocity),
                ThrusterControlState.Settling => CalculateSettlingTorque(error, angularVelocity),
                _ => 0f
            };

            // Clamp to available thruster capacity
            float maxTorque = desiredTorque > 0
                ? _thrusterCoordinator.CounterClockwiseCapacity
                : _thrusterCoordinator.ClockwiseCapacity;
            desiredTorque = Mathf.Clamp(desiredTorque, -maxTorque, maxTorque);

            // Debug logging (throttled or on state change)
            _debugLogTimer += deltaTime;
            if (_debugLogTimer >= 1f || _currentState != previousState)
            {
                _debugLogTimer = 0f;
                float absVelocity = Mathf.Abs(angularVelocity);
                float stopAngle = absVelocity > 0.1f
                    ? absVelocity * absVelocity / (2f * CalculateMaxAngularAcceleration(true, error))
                    : 0f;
                float thrustPercent = maxTorque > 0.001f ? Mathf.Abs(desiredTorque) / maxTorque * 100f : 0f;
                Debug.Log($"[RotationModule] State={_currentState}, error={error:F2}°, angVel={angularVelocity:F2}°/s, stopAngle={stopAngle:F2}°, torque={desiredTorque:F2} ({thrustPercent:F1}%)");
            }

            // Distribute torque to thrusters and apply forces
            _thrusterCoordinator.DistributeTorque(desiredTorque);
            _thrusterCoordinator.UpdateThrusters(deltaTime);
            _thrusterCoordinator.ApplyForces();
            _thrusterCoordinator.UpdateVisuals();

            // Clamp angular velocity
            ClampAngularVelocity();
        }

        #endregion
    }
}
