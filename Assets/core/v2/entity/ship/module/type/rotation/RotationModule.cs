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
        /// Uses bang-bang control: full braking thrust until stopped.
        /// </summary>
        private float CalculateBrakingTorque(float angularVelocity)
        {
            float absVelocity = Mathf.Abs(angularVelocity);

            if (absVelocity < _config.VelocityDeadzone)
            {
                return 0f; // Already stopped
            }

            // Bang-bang: Full braking thrust opposite to velocity direction
            float direction = -Mathf.Sign(angularVelocity);

            float maxTorque = direction > 0
                ? _thrusterCoordinator.CounterClockwiseCapacity
                : _thrusterCoordinator.ClockwiseCapacity;

            return direction * maxTorque;
        }

        /// <summary>
        /// Calculate torque for settling phase using simplified proportional control.
        /// Prevents oscillation by using gentle corrections and coasting when appropriate.
        /// </summary>
        private float CalculateSettlingTorque(float error, float angularVelocity)
        {
            float absError = Mathf.Abs(error);
            float absVelocity = Mathf.Abs(angularVelocity);

            // Accept position as "good enough" - use wider threshold to prevent repeated micro-corrections
            if (absError < _config.SettlingAcceptanceThreshold && absVelocity < _config.VelocityDeadzone)
            {
                return 0f;
            }

            // Check if moving toward target
            bool movingTowardTarget = (error > 0 && angularVelocity > 0) ||
                                       (error < 0 && angularVelocity < 0);

            // Estimate if current velocity will overshoot the target
            // Time to reach target at current velocity: t = error / velocity
            // Use permissive threshold (0.05s) to let momentum carry through more
            float timeToTarget = absVelocity > 0.1f ? absError / absVelocity : float.MaxValue;
            bool willOvershoot = movingTowardTarget && timeToTarget < 0.05f;

            float settlingFactor;

            if (movingTowardTarget && !willOvershoot)
            {
                // Moving toward target with good trajectory - COAST
                settlingFactor = 0f;
            }
            else if (movingTowardTarget && willOvershoot)
            {
                // About to overshoot - apply gentle braking
                float brakeFactor = Mathf.Clamp01(absVelocity / _config.SettlingVelocityThreshold);
                settlingFactor = -Mathf.Sign(angularVelocity) * brakeFactor * 0.5f;
            }
            else
            {
                // Moving away from target or stopped - apply proportional correction with strong burst
                // Use minimum floor of 0.5 so small corrections still get meaningful thrust
                float normalizedError = Mathf.Clamp01(absError / _config.SettlingAngleThreshold);
                normalizedError = Mathf.Max(normalizedError, 0.5f);
                settlingFactor = Mathf.Sign(error) * normalizedError * _config.SettlingProportionalGain * 0.8f;
                settlingFactor = Mathf.Clamp(settlingFactor, -1f, 1f);
            }

            // Get appropriate capacity
            float direction = Mathf.Sign(settlingFactor);
            float maxTorque = direction > 0
                ? _thrusterCoordinator.CounterClockwiseCapacity
                : _thrusterCoordinator.ClockwiseCapacity;

            float desiredTorque = settlingFactor * maxTorque;
            float absTorque = Mathf.Abs(desiredTorque);

            // Apply minimum thrust fraction
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

        /// <summary>
        /// Determine optimal rotation direction by comparing time to reach target.
        /// When spinning fast, sometimes continuing through the long path is faster than braking and reversing.
        /// Returns the optimized angle error (positive = CCW, negative = CW).
        /// </summary>
        private float OptimizeRotationDirection(float shortPathError, float angularVelocity)
        {
            float absVelocity = Mathf.Abs(angularVelocity);

            // Only optimize when:
            // 1. Feature is enabled
            // 2. Ship is spinning fast enough that reversal would be costly
            if (!_config.EnableDirectionOptimization ||
                absVelocity < _config.DirectionOptimizationVelocityThreshold)
            {
                return shortPathError;
            }

            // Check if short path requires direction change
            bool shortPathSameDirection = (shortPathError > 0 && angularVelocity > 0) ||
                                          (shortPathError < 0 && angularVelocity < 0);

            if (shortPathSameDirection)
                return shortPathError; // Already going the right way

            // Calculate long path (continue in current direction)
            // If spinning CCW (positive velocity) and short path is negative (CW), long path = 360 + shortPath
            // If spinning CW (negative velocity) and short path is positive (CCW), long path = -360 + shortPath
            float longPathError = angularVelocity > 0
                ? 360f + shortPathError   // CCW: 360 + (negative short path)
                : -360f + shortPathError; // CW: -360 + (positive short path)

            // Estimate time for each path
            float tShort = EstimateTimeToTarget(shortPathError, angularVelocity, requiresReverse: true);
            float tLong = EstimateTimeToTarget(longPathError, angularVelocity, requiresReverse: false);

            // Choose faster path
            if (tLong < tShort)
            {
                Debug.Log($"[RotationModule] Direction optimization: continuing {(angularVelocity > 0 ? "CCW" : "CW")}. " +
                          $"shortPath={shortPathError:F1}° (t={tShort:F2}s), longPath={longPathError:F1}° (t={tLong:F2}s)");
                return longPathError;
            }

            return shortPathError;
        }

        /// <summary>
        /// Estimate time to reach target angle from current state using kinematic equations.
        /// </summary>
        private float EstimateTimeToTarget(float angleError, float currentVelocity, bool requiresReverse)
        {
            float maxAccel = CalculateMaxAngularAcceleration(forBraking: false, angleError);
            float maxBrake = CalculateMaxAngularAcceleration(forBraking: true, angleError);
            float maxVel = _config.MaxAngularVelocity;

            // Guard against division by zero
            if (maxAccel < 0.001f) maxAccel = 0.001f;
            if (maxBrake < 0.001f) maxBrake = 0.001f;

            float absError = Mathf.Abs(angleError);
            float absVelocity = Mathf.Abs(currentVelocity);

            if (requiresReverse)
            {
                // Path: brake to stop → accelerate in new direction → coast → final brake
                float tBrake = absVelocity / maxBrake;

                // While braking, we travel in the WRONG direction (away from target)
                // Distance covered while braking: d = v*t - 0.5*a*t^2 = v^2 / (2*a)
                float angleCoveredBraking = (absVelocity * absVelocity) / (2f * maxBrake);
                float remainingAngle = absError + angleCoveredBraking;

                // For the remaining angle, use triangle or trapezoid velocity profile
                // Triangle profile time: t = 2 * sqrt(angle / acceleration)
                float tTriangle = 2f * Mathf.Sqrt(remainingAngle / maxAccel);

                // Check if triangle profile would exceed max velocity
                float peakVelocity = maxAccel * (tTriangle / 2f);
                if (peakVelocity > maxVel)
                {
                    // Trapezoid profile: accelerate to max, coast, decelerate
                    float tAccel = maxVel / maxAccel;
                    float angleAccel = 0.5f * maxAccel * tAccel * tAccel;
                    float coastAngle = remainingAngle - 2f * angleAccel;
                    float tCoast = Mathf.Max(0f, coastAngle / maxVel);
                    return tBrake + 2f * tAccel + tCoast;
                }

                return tBrake + tTriangle;
            }
            else
            {
                // Continue in same direction - already have velocity toward target
                bool atMaxVel = absVelocity >= maxVel * 0.95f;

                if (atMaxVel)
                {
                    // Coast most of the way, brake at the end
                    float brakeAngle = (absVelocity * absVelocity) / (2f * maxBrake);
                    float coastAngle = absError - brakeAngle;
                    float tCoast = Mathf.Max(0f, coastAngle / absVelocity);
                    float tBrake = absVelocity / maxBrake;
                    return tCoast + tBrake;
                }
                else
                {
                    // Accelerate toward max, coast, brake
                    float tAccel = (maxVel - absVelocity) / maxAccel;

                    // Distance covered during acceleration: d = v0*t + 0.5*a*t^2
                    float angleAccel = absVelocity * tAccel + 0.5f * maxAccel * tAccel * tAccel;
                    float brakeAngle = (maxVel * maxVel) / (2f * maxBrake);
                    float coastAngle = absError - angleAccel - brakeAngle;

                    if (coastAngle > 0)
                    {
                        // Full trapezoid: accel → coast → brake
                        float tCoast = coastAngle / maxVel;
                        float tBrake = maxVel / maxBrake;
                        return tAccel + tCoast + tBrake;
                    }
                    else
                    {
                        // Short distance - won't reach max velocity
                        // Approximate: time ≈ remaining angle / current velocity
                        // This is conservative but avoids complex math for edge cases
                        return absError / Mathf.Max(absVelocity, 1f);
                    }
                }
            }
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
            var markers = _config.AutoDiscoverThrusters && _controller?.Transform != null
                ? _controller.Transform.GetComponentsInChildren<ThrusterMarker>()
                : new ThrusterMarker[0];

            // Build list of thruster states
            var thrusterList = new System.Collections.Generic.List<ThrusterState>();
            var matchedMarkerIds = new System.Collections.Generic.HashSet<string>();

            // 1. Create states from explicit ThrusterDefinitions
            if (definitions != null && definitions.Length > 0)
            {
                foreach (var def in definitions)
                {
                    var state = new ThrusterState(def);

                    // Try to find matching marker by slotId
                    foreach (var marker in markers)
                    {
                        if (!string.IsNullOrEmpty(def.slotId) && def.slotId == marker.SlotId)
                        {
                            state.Marker = marker;
                            matchedMarkerIds.Add(marker.SlotId);
                            break;
                        }
                    }

                    thrusterList.Add(state);
                    float efficiency = def.CalculateTorqueEfficiency();
                    Debug.Log($"[RotationModule] Thruster (defined): pos={def.localPosition}, dir={def.thrustDirection}, maxThrust={def.maxThrust}, efficiency={efficiency:F3}");
                }
            }

            // 2. Auto-discover unmatched markers and create definitions for them
            foreach (var marker in markers)
            {
                // Skip if already matched to a definition
                if (!string.IsNullOrEmpty(marker.SlotId) && matchedMarkerIds.Contains(marker.SlotId))
                    continue;

                // Create definition from marker
                var def = new ThrusterDefinition
                {
                    slotId = marker.SlotId ?? $"auto_{marker.GetInstanceID()}",
                    localPosition = _controller.Transform.InverseTransformPoint(marker.WorldPosition),
                    thrustDirection = marker.ThrustDirection,
                    maxThrust = marker.MaxThrust > 0 ? marker.MaxThrust : _config.DefaultThrusterThrust,
                    responseTime = _config.DefaultThrusterResponseTime
                };

                var state = new ThrusterState(def);
                state.Marker = marker;
                thrusterList.Add(state);

                float efficiency = def.CalculateTorqueEfficiency();
                Debug.Log($"[RotationModule] Thruster (auto-discovered): pos={def.localPosition}, dir={def.thrustDirection}, maxThrust={def.maxThrust}, efficiency={efficiency:F3}");
            }

            if (thrusterList.Count == 0)
            {
                Debug.LogWarning("[RotationModule] ThrusterBased mode selected but no thrusters found. Place ThrusterMarker components on ship or define thrusters in config.");
                return;
            }

            _thrusterStates = thrusterList.ToArray();
            Debug.Log($"[RotationModule] Initialized {_thrusterStates.Length} thrusters ({definitions?.Length ?? 0} defined, {_thrusterStates.Length - (definitions?.Length ?? 0)} auto-discovered)");

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

                // Orient visual to face exhaust direction (opposite of thrust/force direction)
                Vector2 thrustDir = state.Marker?.ThrustDirection ?? state.Definition.NormalizedThrustDirection;
                Vector2 exhaustDir = -thrustDir; // Exhaust is expelled opposite to the reaction force
                float angle = Mathf.Atan2(exhaustDir.y, exhaustDir.x) * Mathf.Rad2Deg - 90f;

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

            float shortPathError = input.GetAngleDelta(_config.SpriteOffset);
            float angularVelocity = _controller.Rigid2D.angularVelocity;

            // Optimize rotation direction - may choose to continue spinning instead of reversing
            float error = OptimizeRotationDirection(shortPathError, angularVelocity);

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
