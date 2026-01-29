using System.Collections.Generic;
using System.Linq;
using Starfire.Core.V3.Cam.Effects;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Implementation of a point defense module that automatically targets and destroys incoming threats.
    /// Typically used to intercept missiles, torpedoes, and other projectiles.
    /// Can integrate with sensor modules for threat detection or fall back to direct physics queries.
    /// </summary>
    public class PointDefenseModule : IDefensiveWeaponModule
    {
        private readonly PointDefenseModuleConfig _config;
        private IEntityController _controller;
        private V2HardpointMarker _hardpoint;
        private V2WeaponVisual _visual;
        private float _cooldownTimer;
        private float _scanTimer;
        private bool _autoTargetingEnabled = true;
        private Vector2 _manualAimDirection = Vector2.up;
        private Vector2 _lastAimDirection = Vector2.up;

        // Sensor integration
        private ISensorModule _sensorModule;
        private bool _usingSensorData;

        private readonly List<Transform> _trackedTargets = new();
        private readonly List<V2DetectedEntity> _sensorTrackedThreats = new();
        private Transform _currentTarget;
        private V2DetectedEntity? _currentSensorTarget;

        // IEntityModule
        public string ModuleId => _config.ModuleId;
        public bool IsEnabled { get; set; } = true;

        // IShipModule
        public ShipModuleCategory Category => ShipModuleCategory.Defense;
        public ShipModuleType Type => ShipModuleType.PointDefense;

        // IWeaponModule
        public WeaponWeightClass WeightClass => _config.WeightClass;
        public float FireRate => _config.FireRate;
        public float Range => _config.Range;
        public bool IsTurret => true; // Point defense is always a turret
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

        public bool CanFire
        {
            get
            {
                if (!IsEnabled) return false;
                if (_cooldownTimer > 0f) return false;

                // Don't fire until turret is aimed at target
                if (_visual != null && !_visual.IsAimedAtTarget())
                    return false;

                return true;
            }
        }

        // IDefensiveWeaponModule
        public float EngagementRange => _config.EngagementRange;
        public float TrackingSpeed => _config.TrackingSpeed;
        public bool IsEngaged => _currentTarget != null;

        public PointDefenseModule(PointDefenseModuleConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Checks if a Unity Transform reference is valid (not null and not destroyed).
        /// Uses Unity's implicit bool operator which catches both null and destroyed objects,
        /// preventing access to destroyed objects that return (0,0) for position.
        /// </summary>
        private static bool IsValidTarget(Transform target)
        {
            if (!(bool)target) return false;
            // Pooled projectiles are still valid Transforms but their GameObject is inactive
            // and their position is reset to (0,0). Check activeInHierarchy to filter them.
            return target.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Checks if a transform belongs to a projectile owned by this module's controller.
        /// Prevents point defense from targeting its own ship's projectiles.
        /// </summary>
        private bool IsOwnProjectile(Transform target)
        {
            if (_controller == null || target == null) return false;
            var projectile = target.GetComponent<V2Projectile>();
            return projectile != null && projectile.Owner == _controller;
        }

        public void OnAttach(IEntityController controller)
        {
            _controller = controller;
            // Sensor resolution is deferred to OnUpdate via TryResolveSensor()
            // to avoid initialization order dependencies between module slots.
        }

        /// <summary>
        /// Lazily resolves the sensor module reference. Called once per update until found.
        /// This avoids depending on module registration order in ShipEntity.InitializeModules().
        /// </summary>
        private void TryResolveSensor()
        {
            if (_sensorModule != null || !_config.UseSensorIntegration) return;
            if (_controller is not ShipController shipController) return;

            _sensorModule = shipController.Ship?.Modules
                ?.GetAllModulesOfType<ISensorModule>()
                ?.FirstOrDefault();

            _usingSensorData = _sensorModule != null;
        }

        public void OnDetach()
        {
            _sensorModule = null;
            _usingSensorData = false;
            OnHardpointUnassigned();
            _controller = null;
            _trackedTargets.Clear();
            _sensorTrackedThreats.Clear();
            _currentTarget = null;
            _currentSensorTarget = null;
        }

        public void OnUpdate(float deltaTime)
        {
            TryResolveSensor();

            // Update cooldown
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= deltaTime;
            }

            // Auto-targeting logic
            if (_autoTargetingEnabled && _controller != null)
            {
                // Both sensor and physics paths use the scan timer to avoid per-frame queries
                _scanTimer -= deltaTime;

                if (_usingSensorData && _sensorModule != null)
                {
                    if (_scanTimer <= 0f)
                    {
                        UpdateSensorBasedTargeting();
                        _scanTimer = _config.ScanInterval;
                    }
                    else
                    {
                        // Between scans, just clean up destroyed targets and reselect
                        CleanupSensorTargets();
                        SelectSensorPriorityTarget(_controller.Transform.position);
                    }
                }
                else
                {
                    if (_scanTimer <= 0f)
                    {
                        ScanForThreats();
                        _scanTimer = _config.ScanInterval;
                    }

                    CleanupInvalidTargets();
                    SelectPriorityTarget();
                }

                // Auto-fire at current target (validate target is still alive)
                if (IsValidTarget(_currentTarget) && CanFire)
                {
                    UpdateAimToTarget();

                    if (_visual == null || _visual.IsAimedAtTarget())
                    {
                        Fire();
                    }
                }
                else if (!IsValidTarget(_currentTarget))
                {
                    _currentTarget = null;
                }
            }

            // Update turret visual
            if (_visual != null)
            {
                if (_autoTargetingEnabled && IsValidTarget(_currentTarget))
                {
                    Vector2 newDir = GetLeadDirection(_currentTarget);
                    // Only update if direction is valid (non-zero)
                    if (newDir.sqrMagnitude > 0.001f)
                    {
                        _lastAimDirection = newDir;
                    }
                }

                // Use last known aim direction to prevent snapping to origin on target loss
                _visual.SetTargetDirection(_lastAimDirection);
            }
        }

        /// <summary>
        /// Updates targeting using sensor data. Only called on scan interval.
        /// </summary>
        private void UpdateSensorBasedTargeting()
        {
            if (_sensorModule == null || _controller == null) return;

            Vector2 position = _controller.Transform.position;

            // Query all detected threats from sensor, then filter by engagement range
            // using live distance from our position (not the sensor's cached Distance field,
            // which may be stale or computed from a different position).
            var threats = _sensorModule.GetDetectedThreats();

            // Rebuild tracked threats list from snapshot
            _sensorTrackedThreats.Clear();
            foreach (var threat in threats)
            {
                if (!IsValidTarget(threat.Transform)) continue;
                if (threat.Transform.IsChildOf(_controller.Transform)) continue;
                if (IsOwnProjectile(threat.Transform)) continue;

                float liveDist = Vector2.Distance(position, threat.Transform.position);
                if (liveDist > _config.EngagementRange) continue;

                _sensorTrackedThreats.Add(threat);
            }

            // Clean up - remove destroyed targets
            CleanupSensorTargets();

            // Select priority target from sensor data
            SelectSensorPriorityTarget(position);
        }

        /// <summary>
        /// Removes invalid targets from the sensor tracking list.
        /// </summary>
        private void CleanupSensorTargets()
        {
            for (int i = _sensorTrackedThreats.Count - 1; i >= 0; i--)
            {
                if (!IsValidTarget(_sensorTrackedThreats[i].Transform))
                {
                    _sensorTrackedThreats.RemoveAt(i);
                }
            }

            // Clear current target if invalid
            if (_currentSensorTarget.HasValue && !IsValidTarget(_currentSensorTarget.Value.Transform))
            {
                _currentSensorTarget = null;
                _currentTarget = null;
            }
        }

        /// <summary>
        /// Selects the highest priority threat from sensor data.
        /// </summary>
        private void SelectSensorPriorityTarget(Vector2 position)
        {
            if (_sensorTrackedThreats.Count == 0)
            {
                _currentSensorTarget = null;
                _currentTarget = null;
                return;
            }

            // Priority: closest threat within firing range
            V2DetectedEntity? closest = null;
            float closestDist = float.MaxValue;

            foreach (var threat in _sensorTrackedThreats)
            {
                if (!IsValidTarget(threat.Transform)) continue;

                float dist = Vector2.Distance(position, threat.Transform.position);

                if (dist <= Range && dist < closestDist)
                {
                    closestDist = dist;
                    closest = threat;
                }
            }

            _currentSensorTarget = closest;
            _currentTarget = closest?.Transform;
        }

        public void OnHardpointAssigned(V2HardpointMarker hardpoint)
        {
            if (hardpoint == null) return;

            // Validate weight class compatibility
            if (!hardpoint.CanMount(WeightClass))
            {
                Debug.LogWarning($"Cannot mount {WeightClass} weapon '{_config.DisplayName}' on {hardpoint.WeightClass} hardpoint '{hardpoint.SlotId}'");
                return;
            }

            _hardpoint = hardpoint;

            // Instantiate visual if we have a prefab
            if (_config.WeaponVisualPrefab != null)
            {
                var visualGO = Object.Instantiate(
                    _config.WeaponVisualPrefab,
                    _hardpoint.MountPoint
                );
                visualGO.transform.localPosition = Vector3.zero;
                visualGO.transform.localRotation = Quaternion.identity;

                _visual = visualGO.GetComponent<V2WeaponVisual>();
                if (_visual != null)
                {
                    _visual.Initialize(_config.TurretSettings);
                }
                else
                {
                    Debug.LogWarning($"Weapon visual prefab '{_config.WeaponVisualPrefab.name}' is missing V2WeaponVisual component");
                }
            }
        }

        public void OnHardpointUnassigned()
        {
            if (_visual != null)
            {
                Object.Destroy(_visual.gameObject);
                _visual = null;
            }

            _hardpoint = null;
        }

        public void SetAimDirection(Vector2 worldDirection)
        {
            if (worldDirection.sqrMagnitude > 0.001f)
            {
                _manualAimDirection = worldDirection.normalized;
            }
        }

        public bool Fire()
        {
            if (!CanFire) return false;

            // Set cooldown
            _cooldownTimer = 1f / FireRate;

            // Spawn projectile
            SpawnProjectile();

            return true;
        }

        public void SetAutoTargetingEnabled(bool enabled)
        {
            _autoTargetingEnabled = enabled;

            if (!enabled)
            {
                _currentTarget = null;
            }
        }

        public int GetTrackedTargetCount()
        {
            return _trackedTargets.Count;
        }

        private void ScanForThreats()
        {
            if (_controller == null) return;

            Vector2 position = _controller.Transform.position;

            // Find all threats in engagement range
            var results = Physics2D.OverlapCircleAll(
                position,
                _config.EngagementRange,
                _config.ThreatLayers
            );

            // Add new threats to tracking list
            foreach (var collider in results)
            {
                if (collider == null) continue;

                // Skip if already tracking
                if (_trackedTargets.Contains(collider.transform)) continue;

                // Skip if owned by us
                if (collider.transform.IsChildOf(_controller.Transform)) continue;
                if (IsOwnProjectile(collider.transform)) continue;

                // Add to tracking (up to max)
                if (_trackedTargets.Count < _config.MaxTrackedTargets)
                {
                    _trackedTargets.Add(collider.transform);
                }
            }
        }

        private void CleanupInvalidTargets()
        {
            if (_controller == null) return;

            Vector2 position = _controller.Transform.position;

            for (int i = _trackedTargets.Count - 1; i >= 0; i--)
            {
                var target = _trackedTargets[i];

                // Remove destroyed targets
                if (!IsValidTarget(target))
                {
                    _trackedTargets.RemoveAt(i);
                    continue;
                }

                // Remove targets out of engagement range
                float distance = Vector2.Distance(position, target.position);
                if (distance > _config.EngagementRange)
                {
                    _trackedTargets.RemoveAt(i);
                    continue;
                }
            }

            // Clear current target if invalid
            if (!IsValidTarget(_currentTarget) || !_trackedTargets.Contains(_currentTarget))
            {
                _currentTarget = null;
            }
        }

        private void SelectPriorityTarget()
        {
            if (_trackedTargets.Count == 0)
            {
                _currentTarget = null;
                return;
            }

            if (_controller == null) return;

            Vector2 position = _controller.Transform.position;

            // Priority: closest target
            Transform closest = null;
            float closestDist = float.MaxValue;

            foreach (var target in _trackedTargets)
            {
                if (!IsValidTarget(target)) continue;

                float dist = Vector2.Distance(position, target.position);

                // Only consider targets within firing range
                if (dist <= Range && dist < closestDist)
                {
                    closestDist = dist;
                    closest = target;
                }
            }

            _currentTarget = closest;
        }

        private void UpdateAimToTarget()
        {
            if (!IsValidTarget(_currentTarget) || _controller == null) return;

            Vector2 leadDir = GetLeadDirection(_currentTarget);

            if (_visual != null && leadDir.sqrMagnitude > 0.001f)
            {
                _visual.SetTargetDirection(leadDir);
            }
        }

        private Vector2 GetLeadDirection(Transform target)
        {
            if (_controller == null || !IsValidTarget(target))
            {
                return _lastAimDirection;
            }

            Vector2 myPosition = GetFirePosition();
            Vector2 targetPosition = target.position;

            // Validate target position isn't at origin due to destroyed object
            Vector2 toTarget = targetPosition - myPosition;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                return _lastAimDirection;
            }

            // Calculate intercept if prediction is enabled
            if (_config.TurretSettings != null && _config.TurretSettings.predictTargetPosition)
            {
                var targetRb = target.GetComponent<Rigidbody2D>();
                if (targetRb != null && _config.ProjectileConfig != null)
                {
                    Vector2 shooterVelocity = _controller.Rigid2D != null
                        ? _controller.Rigid2D.linearVelocity
                        : Vector2.zero;
                    Vector2 targetVelocity = targetRb.linearVelocity;
                    float projectileSpeed = _config.ProjectileConfig.speed;
                    bool inheritVelocity = _config.ProjectileConfig.inheritVelocity;

                    Vector2? interceptDir = CalculateInterceptDirection(
                        myPosition,
                        shooterVelocity,
                        targetPosition,
                        targetVelocity,
                        projectileSpeed,
                        inheritVelocity
                    );

                    if (interceptDir.HasValue && interceptDir.Value.sqrMagnitude > 0.001f)
                    {
                        return interceptDir.Value;
                    }
                }
            }

            return toTarget.normalized;
        }

        /// <summary>
        /// Calculates the direction to fire to intercept a moving target.
        /// Uses quadratic solution accounting for both shooter and target velocities.
        /// </summary>
        private Vector2? CalculateInterceptDirection(
            Vector2 shooterPos,
            Vector2 shooterVelocity,
            Vector2 targetPos,
            Vector2 targetVelocity,
            float projectileSpeed,
            bool inheritVelocity)
        {
            // Relative position: target relative to shooter
            Vector2 relativePos = targetPos - shooterPos;

            // Relative velocity depends on whether projectile inherits shooter velocity
            // If inheritVelocity = true: projectile moves at 'speed' relative to shooter
            //   so we work in shooter's reference frame, using relative velocity
            // If inheritVelocity = false: projectile moves at 'speed' in world space
            //   so we use target's absolute velocity
            Vector2 relativeVel = inheritVelocity
                ? (targetVelocity - shooterVelocity)
                : targetVelocity;

            // Solve: |relativePos + relativeVel * t| = projectileSpeed * t
            // Expanding: (R + V*t)·(R + V*t) = s²t²
            // R·R + 2(R·V)t + (V·V)t² = s²t²
            // (V·V - s²)t² + 2(R·V)t + R·R = 0

            float a = Vector2.Dot(relativeVel, relativeVel) - projectileSpeed * projectileSpeed;
            float b = 2f * Vector2.Dot(relativePos, relativeVel);
            float c = Vector2.Dot(relativePos, relativePos);

            float discriminant = b * b - 4f * a * c;

            // No real solution - target is unreachable
            if (discriminant < 0f)
            {
                return null;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float t1, t2;

            // Handle special case where a ≈ 0 (projectile speed equals relative velocity magnitude)
            if (Mathf.Abs(a) < 0.0001f)
            {
                if (Mathf.Abs(b) < 0.0001f)
                {
                    return null; // Degenerate case
                }
                t1 = t2 = -c / b;
            }
            else
            {
                t1 = (-b + sqrtDiscriminant) / (2f * a);
                t2 = (-b - sqrtDiscriminant) / (2f * a);
            }

            // Choose smallest positive time
            float interceptTime;
            if (t1 > 0.001f && t2 > 0.001f)
            {
                interceptTime = Mathf.Min(t1, t2);
            }
            else if (t1 > 0.001f)
            {
                interceptTime = t1;
            }
            else if (t2 > 0.001f)
            {
                interceptTime = t2;
            }
            else
            {
                return null; // No positive solution - target is behind us or unreachable
            }

            // Calculate intercept point and direction
            Vector2 interceptPoint = relativePos + relativeVel * interceptTime;

            if (interceptPoint.sqrMagnitude < 0.0001f)
            {
                return null; // Target is at our position
            }

            return interceptPoint.normalized;
        }

        /// <summary>
        /// Applies accuracy-based spread to a firing direction.
        /// </summary>
        private Vector2 ApplyAccuracySpread(Vector2 perfectDirection, float distanceToTarget)
        {
            // Calculate effective accuracy (0-1 range)
            float effectiveAccuracy = _config.AccuracyPercent / 100f;

            // Apply range-based accuracy decay if enabled
            if (_config.AccuracyDecayOverRange && _config.RangeAccuracyFalloff != null)
            {
                float rangeRatio = Mathf.Clamp01(distanceToTarget / _config.Range);
                effectiveAccuracy *= _config.RangeAccuracyFalloff.Evaluate(rangeRatio);
            }

            // Calculate max spread based on accuracy (0% accuracy = max spread, 100% = no spread)
            float maxSpreadRadians = _config.MaxSpreadAngle * Mathf.Deg2Rad * (1f - effectiveAccuracy);

            if (maxSpreadRadians < 0.0001f)
            {
                return perfectDirection;
            }

            // Apply random angular deviation
            float randomAngle = Random.Range(-maxSpreadRadians, maxSpreadRadians);

            // Rotate direction by random angle
            float cos = Mathf.Cos(randomAngle);
            float sin = Mathf.Sin(randomAngle);
            return new Vector2(
                perfectDirection.x * cos - perfectDirection.y * sin,
                perfectDirection.x * sin + perfectDirection.y * cos
            ).normalized;
        }

        private Vector2 GetFirePosition()
        {
            if (_visual != null)
            {
                return _visual.GetMuzzlePosition();
            }
            else if (_hardpoint != null)
            {
                return _hardpoint.MountPoint.position;
            }
            else if (_controller != null)
            {
                return _controller.Transform.position;
            }

            return Vector2.zero;
        }

        private void SpawnProjectile()
        {
            var projConfig = _config.ProjectileConfig;
            if (projConfig == null)
            {
                Debug.LogWarning($"Point defense '{_config.DisplayName}' has no ProjectileConfig assigned");
                return;
            }

            // Physics mode requires a prefab
            if (projConfig.mode == V2ProjectileMode.Physics && _config.ProjectilePrefab == null)
            {
                Debug.LogWarning($"Point defense '{_config.DisplayName}' has no projectile prefab for Physics mode");
                return;
            }

            // Determine spawn position and direction
            Vector2 spawnPos;
            Vector2 direction;
            float distanceToTarget = 0f;

            if (_visual != null)
            {
                spawnPos = _visual.GetMuzzlePosition();
                direction = _visual.GetMuzzleDirection();
                if (IsValidTarget(_currentTarget))
                {
                    distanceToTarget = Vector2.Distance(spawnPos, _currentTarget.position);
                }
            }
            else if (_hardpoint != null)
            {
                spawnPos = _hardpoint.MountPoint.position;
                if (IsValidTarget(_currentTarget))
                {
                    direction = GetLeadDirection(_currentTarget);
                    distanceToTarget = Vector2.Distance(spawnPos, _currentTarget.position);
                }
                else
                {
                    direction = _hardpoint.WorldFiringDirection;
                }
            }
            else if (_controller != null)
            {
                spawnPos = _controller.Transform.position;
                if (IsValidTarget(_currentTarget))
                {
                    direction = GetLeadDirection(_currentTarget);
                    distanceToTarget = Vector2.Distance(spawnPos, _currentTarget.position);
                }
                else
                {
                    direction = _lastAimDirection;
                }
            }
            else
            {
                Debug.LogWarning($"Cannot spawn projectile for '{_config.DisplayName}': no position reference available");
                return;
            }

            // Apply accuracy spread to the firing direction
            direction = ApplyAccuracySpread(direction, distanceToTarget);

            // Calculate inherited velocity if enabled
            Vector2 inheritedVelocity = Vector2.zero;
            if (projConfig.inheritVelocity && _controller?.Rigid2D != null)
            {
                inheritedVelocity = _controller.Rigid2D.linearVelocity;
            }

            // Build spawn context and delegate to spawner
            var context = new V2ProjectileSpawnContext
            {
                Owner = _controller,
                SpawnPosition = spawnPos,
                Direction = direction,
                InheritedVelocity = inheritedVelocity,
                Damage = _config.Damage,
                DamageConfig = null, // Point defense typically uses default damage
                ProjectileConfig = projConfig,
                ProjectilePrefab = _config.ProjectilePrefab
            };

            V2ProjectileSpawner.Spawn(context);

            // Trigger fire shake (recoil)
            if (_config.FireShakeConfig != null)
            {
                V3CameraShakeService.Instance?.TriggerFireShake(spawnPos, direction, _config.FireShakeConfig);
            }
        }
    }
}
