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
        private readonly PointDefenseModuleData _data;
        private IEntityController _controller;
        private V2HardpointMarker _hardpoint;
        private V2WeaponVisual _visual;
        private float _cooldownTimer;
        private float _scanTimer;
        private int _burstShotsRemaining;
        private float _burstIntervalTimer;
        private bool _autoTargetingEnabled = true;
        private Vector2 _manualAimDirection = Vector2.up;
        private Vector2 _lastAimDirection = Vector2.up;

        // Sensor integration
        private ISensorModule _sensorModule;
        private bool _usingSensorData;

        // Multi-PD coordination
        private PointDefenseCoordinator _coordinator;

        private readonly List<Transform> _trackedTargets = new();
        private readonly List<V2DetectedEntity> _sensorTrackedThreats = new();
        private Transform _currentTarget;
        private V2DetectedEntity? _currentSensorTarget;

        // Shared buffer for non-allocating physics queries
        private static readonly Collider2D[] _scanBuffer = new Collider2D[64];

        // IEntityModule
        public string ModuleId => _data.moduleId;
        public bool IsEnabled { get; set; } = true;

        // IShipModule
        public ShipModuleCategory Category => ShipModuleCategory.Defense;
        public ShipModuleType Type => ShipModuleType.PointDefense;

        // IWeaponModule
        public WeaponWeightClass WeightClass => _data.weightClass;
        public float FireRate => _data.fireRate;
        public float Range => _data.range;
        public bool IsTurret => true; // Point defense is always a turret
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

        public bool CanFire
        {
            get
            {
                if (!IsEnabled) return false;

                // During a burst, use burst interval timer
                if (_data.useBurstFire && _burstShotsRemaining > 0)
                {
                    if (_burstIntervalTimer > 0f) return false;
                }
                else if (_cooldownTimer > 0f)
                {
                    return false;
                }

                // Don't fire until turret is aimed at target
                if (_visual != null && !_visual.IsAimedAtTarget())
                    return false;

                return true;
            }
        }

        // IDefensiveWeaponModule
        public float EngagementRange => _data.engagementRange;
        public float TrackingSpeed => _data.trackingSpeed;
        public bool IsEngaged => _currentTarget != null;

        public PointDefenseModule(PointDefenseModuleData data)
        {
            _data = data;
        }

        /// <summary>
        /// Checks if a Unity Transform reference is valid (not null and not destroyed).
        /// </summary>
        private static bool IsValidTarget(Transform target)
        {
            if (!(bool)target) return false;
            return target.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Checks if a projectile is on a collision course with the ship.
        /// </summary>
        private static bool IsOnCollisionCourse(
            Vector2 projectilePos,
            Vector2 projectileVelocity,
            Vector2 shipPos,
            float shipRadius)
        {
            Vector2 toShip = shipPos - projectilePos;
            float velSqr = Vector2.Dot(projectileVelocity, projectileVelocity);
            if (velSqr < 0.001f) return false;

            float t = Vector2.Dot(toShip, projectileVelocity) / velSqr;
            if (t < 0f) return false;

            Vector2 closestPoint = projectilePos + projectileVelocity * t;
            float distSqr = (shipPos - closestPoint).sqrMagnitude;
            return distSqr <= shipRadius * shipRadius;
        }

        private static bool IsGuidedThreat(V2DetectedEntityType type)
        {
            return type == V2DetectedEntityType.Missile
                || type == V2DetectedEntityType.Torpedo
                || type == V2DetectedEntityType.Mine;
        }

        private bool IsOwnProjectile(Transform target)
        {
            if (_controller == null || target == null) return false;
            var projectile = target.GetComponent<V2Projectile>();
            return projectile != null && projectile.Owner == _controller;
        }

        public void OnAttach(IEntityController controller)
        {
            _controller = controller;
        }

        private void TryResolveSensor()
        {
            if (_sensorModule != null || !_data.useSensorIntegration) return;
            if (_controller is not ShipController shipController) return;

            _sensorModule = shipController.Ship?.Modules
                ?.GetAllModulesOfType<ISensorModule>()
                ?.FirstOrDefault();

            _usingSensorData = _sensorModule != null;

            if (_coordinator == null && _controller != null)
            {
                _coordinator = PointDefenseCoordinator.GetOrCreate(_controller);
                _coordinator.Register(this);
            }
        }

        public void OnDetach()
        {
            if (_coordinator != null)
            {
                _coordinator.Unregister(this);
                PointDefenseCoordinator.TryRemove(_controller);
                _coordinator = null;
            }

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

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= deltaTime;
            }

            if (_burstIntervalTimer > 0f)
            {
                _burstIntervalTimer -= deltaTime;
            }

            if (_autoTargetingEnabled && _controller != null)
            {
                _scanTimer -= deltaTime;

                if (_usingSensorData && _sensorModule != null)
                {
                    if (_scanTimer <= 0f)
                    {
                        UpdateSensorBasedTargeting();
                        _scanTimer = _data.scanInterval;
                    }
                    else
                    {
                        CleanupSensorTargets();
                        SelectSensorPriorityTarget(_controller.Transform.position);
                    }
                }
                else
                {
                    if (_scanTimer <= 0f)
                    {
                        ScanForThreats();
                        _scanTimer = _data.scanInterval;
                    }

                    CleanupInvalidTargets();
                    SelectPriorityTarget();
                }

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

            if (_visual != null)
            {
                if (_autoTargetingEnabled && IsValidTarget(_currentTarget))
                {
                    Vector2 newDir = GetLeadDirection(_currentTarget);
                    if (newDir.sqrMagnitude > 0.001f)
                    {
                        _lastAimDirection = newDir;
                    }
                }

                _visual.SetTargetDirection(_lastAimDirection);
            }
        }

        private void UpdateSensorBasedTargeting()
        {
            if (_sensorModule == null || _controller == null) return;

            Vector2 position = _controller.Transform.position;

            var mode = _data.targetingMode;
            _sensorTrackedThreats.Clear();

            if (mode != PointDefenseTargetingMode.Offensive)
            {
                var threats = _sensorModule.GetDetectedThreats();
                foreach (var threat in threats)
                {
                    if (!IsValidTarget(threat.Transform)) continue;
                    if (threat.Transform.IsChildOf(_controller.Transform)) continue;
                    if (IsOwnProjectile(threat.Transform)) continue;

                    float liveDist = Vector2.Distance(position, threat.Transform.position);
                    if (liveDist > _data.engagementRange) continue;

                    if (threat.EntityType == V2DetectedEntityType.Projectile)
                    {
                        Vector2 vel = threat.Velocity;
                        if (!IsOnCollisionCourse(threat.Position, vel, position, _data.collisionCourseRadius))
                            continue;
                    }

                    _sensorTrackedThreats.Add(threat);
                }
            }

            if (mode != PointDefenseTargetingMode.Defensive)
            {
                var hostiles = _sensorModule.GetHostileEntities();
                foreach (var hostile in hostiles)
                {
                    if (!IsValidTarget(hostile.Transform)) continue;
                    if (hostile.Transform.IsChildOf(_controller.Transform)) continue;

                    float liveDist = Vector2.Distance(position, hostile.Transform.position);
                    if (liveDist > _data.engagementRange) continue;

                    _sensorTrackedThreats.Add(hostile);
                }
            }

            CleanupSensorTargets();
            SelectSensorPriorityTarget(position);
        }

        private void CleanupSensorTargets()
        {
            for (int i = _sensorTrackedThreats.Count - 1; i >= 0; i--)
            {
                if (!IsValidTarget(_sensorTrackedThreats[i].Transform))
                {
                    _sensorTrackedThreats.RemoveAt(i);
                }
            }

            if (_currentSensorTarget.HasValue && !IsValidTarget(_currentSensorTarget.Value.Transform))
            {
                _currentSensorTarget = null;
                _currentTarget = null;
            }
        }

        private void SelectSensorPriorityTarget(Vector2 position)
        {
            if (_sensorTrackedThreats.Count == 0)
            {
                _currentSensorTarget = null;
                _currentTarget = null;
                ReportEngagementToCoordinator(null);
                return;
            }

            V2DetectedEntity? bestGuided = null;
            float bestGuidedDist = float.MaxValue;
            V2DetectedEntity? bestGuidedUnengaged = null;
            float bestGuidedUnengagedDist = float.MaxValue;

            V2DetectedEntity? bestProjectile = null;
            float bestProjectileDist = float.MaxValue;
            V2DetectedEntity? bestProjectileUnengaged = null;
            float bestProjectileUnengagedDist = float.MaxValue;

            V2DetectedEntity? bestHostile = null;
            float bestHostileDist = float.MaxValue;
            V2DetectedEntity? bestHostileUnengaged = null;
            float bestHostileUnengagedDist = float.MaxValue;

            foreach (var entity in _sensorTrackedThreats)
            {
                if (!IsValidTarget(entity.Transform)) continue;

                float dist = Vector2.Distance(position, entity.Transform.position);
                if (dist > Range) continue;

                bool engaged = _coordinator != null
                    && _coordinator.IsTargetEngagedByOther(this, entity.Transform);

                if (IsGuidedThreat(entity.EntityType))
                {
                    if (dist < bestGuidedDist)
                    {
                        bestGuidedDist = dist;
                        bestGuided = entity;
                    }
                    if (!engaged && dist < bestGuidedUnengagedDist)
                    {
                        bestGuidedUnengagedDist = dist;
                        bestGuidedUnengaged = entity;
                    }
                }
                else if (entity.EntityType == V2DetectedEntityType.Projectile)
                {
                    if (dist < bestProjectileDist)
                    {
                        bestProjectileDist = dist;
                        bestProjectile = entity;
                    }
                    if (!engaged && dist < bestProjectileUnengagedDist)
                    {
                        bestProjectileUnengagedDist = dist;
                        bestProjectileUnengaged = entity;
                    }
                }
                else
                {
                    if (dist < bestHostileDist)
                    {
                        bestHostileDist = dist;
                        bestHostile = entity;
                    }
                    if (!engaged && dist < bestHostileUnengagedDist)
                    {
                        bestHostileUnengagedDist = dist;
                        bestHostileUnengaged = entity;
                    }
                }
            }

            var selected = bestGuidedUnengaged ?? bestGuided
                ?? bestProjectileUnengaged ?? bestProjectile
                ?? bestHostileUnengaged ?? bestHostile;

            _currentSensorTarget = selected;
            _currentTarget = selected?.Transform;
            ReportEngagementToCoordinator(selected?.Transform);
        }

        private void ReportEngagementToCoordinator(Transform target)
        {
            _coordinator?.ReportEngagement(this, target);
        }

        public void OnHardpointAssigned(V2HardpointMarker hardpoint)
        {
            if (hardpoint == null) return;

            if (!hardpoint.CanMount(WeightClass))
            {
                Debug.LogWarning($"Cannot mount {WeightClass} weapon '{_data.displayName}' on {hardpoint.WeightClass} hardpoint '{hardpoint.SlotId}'");
                return;
            }

            _hardpoint = hardpoint;

            if (_data.weaponVisualPrefab != null)
            {
                var visualGO = Object.Instantiate(
                    _data.weaponVisualPrefab,
                    _hardpoint.MountPoint
                );
                visualGO.transform.localPosition = Vector3.zero;
                visualGO.transform.localRotation = Quaternion.identity;

                _visual = visualGO.GetComponent<V2WeaponVisual>();
                if (_visual != null)
                {
                    _visual.Initialize(_data.turretSettings);
                }
                else
                {
                    Debug.LogWarning($"Weapon visual prefab '{_data.weaponVisualPrefab.name}' is missing V2WeaponVisual component");
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

            SpawnProjectile();

            if (_data.useBurstFire)
            {
                // Start a new burst if not already bursting
                if (_burstShotsRemaining <= 0)
                {
                    _burstShotsRemaining = _data.burstCount - 1; // -1 because we just fired one
                }
                else
                {
                    _burstShotsRemaining--;
                }

                if (_burstShotsRemaining > 0)
                {
                    _burstIntervalTimer = _data.burstInterval;
                }
                else
                {
                    // Burst complete, apply burst cooldown
                    _cooldownTimer = _data.burstCooldown;
                }
            }
            else
            {
                _cooldownTimer = 1f / FireRate;
            }

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

            int count = Physics2D.OverlapCircleNonAlloc(
                position,
                _data.engagementRange,
                _scanBuffer,
                _data.threatLayers
            );

            for (int i = 0; i < count; i++)
            {
                var collider = _scanBuffer[i];
                if (collider == null) continue;
                if (_trackedTargets.Contains(collider.transform)) continue;
                if (collider.transform.IsChildOf(_controller.Transform)) continue;
                if (IsOwnProjectile(collider.transform)) continue;

                if (_trackedTargets.Count < _data.maxTrackedTargets)
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

                if (!IsValidTarget(target))
                {
                    _trackedTargets.RemoveAt(i);
                    continue;
                }

                float distance = Vector2.Distance(position, target.position);
                if (distance > _data.engagementRange)
                {
                    _trackedTargets.RemoveAt(i);
                    continue;
                }
            }

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

            Transform closest = null;
            float closestDist = float.MaxValue;

            foreach (var target in _trackedTargets)
            {
                if (!IsValidTarget(target)) continue;

                float dist = Vector2.Distance(position, target.position);

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

            Vector2 toTarget = targetPosition - myPosition;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                return _lastAimDirection;
            }

            if (_data.turretSettings != null && _data.turretSettings.predictTargetPosition)
            {
                var targetRb = target.GetComponent<Rigidbody2D>();
                if (targetRb != null && _data.projectileConfig != null)
                {
                    Vector2 shooterVelocity = _controller.Rigid2D != null
                        ? _controller.Rigid2D.linearVelocity
                        : Vector2.zero;
                    Vector2 targetVelocity = targetRb.linearVelocity;
                    float projectileSpeed = _data.projectileConfig.speed;
                    bool inheritVelocity = _data.projectileConfig.inheritVelocity;

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

        private Vector2? CalculateInterceptDirection(
            Vector2 shooterPos,
            Vector2 shooterVelocity,
            Vector2 targetPos,
            Vector2 targetVelocity,
            float projectileSpeed,
            bool inheritVelocity)
        {
            Vector2 relativePos = targetPos - shooterPos;

            Vector2 relativeVel = inheritVelocity
                ? (targetVelocity - shooterVelocity)
                : targetVelocity;

            float a = Vector2.Dot(relativeVel, relativeVel) - projectileSpeed * projectileSpeed;
            float b = 2f * Vector2.Dot(relativePos, relativeVel);
            float c = Vector2.Dot(relativePos, relativePos);

            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0f)
            {
                return null;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float t1, t2;

            if (Mathf.Abs(a) < 0.0001f)
            {
                if (Mathf.Abs(b) < 0.0001f)
                {
                    return null;
                }
                t1 = t2 = -c / b;
            }
            else
            {
                t1 = (-b + sqrtDiscriminant) / (2f * a);
                t2 = (-b - sqrtDiscriminant) / (2f * a);
            }

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
                return null;
            }

            Vector2 interceptPoint = relativePos + relativeVel * interceptTime;

            if (interceptPoint.sqrMagnitude < 0.0001f)
            {
                return null;
            }

            return interceptPoint.normalized;
        }

        private Vector2 ApplyAccuracySpread(Vector2 perfectDirection, float distanceToTarget)
        {
            float effectiveAccuracy = _data.accuracyPercent / 100f;

            if (_data.accuracyDecayOverRange && _data.rangeAccuracyFalloff != null)
            {
                float rangeRatio = Mathf.Clamp01(distanceToTarget / _data.range);
                effectiveAccuracy *= _data.rangeAccuracyFalloff.Evaluate(rangeRatio);
            }

            float maxSpreadRadians = _data.maxSpreadAngle * Mathf.Deg2Rad * (1f - effectiveAccuracy);

            if (maxSpreadRadians < 0.0001f)
            {
                return perfectDirection;
            }

            float randomAngle = Random.Range(-maxSpreadRadians, maxSpreadRadians);

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
            var projConfig = _data.projectileConfig;
            if (projConfig == null)
            {
                Debug.LogWarning($"Point defense '{_data.displayName}' has no ProjectileConfig assigned");
                return;
            }

            if (projConfig.mode == V2ProjectileMode.Physics && _data.projectilePrefab == null)
            {
                Debug.LogWarning($"Point defense '{_data.displayName}' has no projectile prefab for Physics mode");
                return;
            }

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
                Debug.LogWarning($"Cannot spawn projectile for '{_data.displayName}': no position reference available");
                return;
            }

            direction = ApplyAccuracySpread(direction, distanceToTarget);

            Vector2 inheritedVelocity = Vector2.zero;
            if (projConfig.inheritVelocity && _controller?.Rigid2D != null)
            {
                inheritedVelocity = _controller.Rigid2D.linearVelocity;
            }

            var context = new V2ProjectileSpawnContext
            {
                Owner = _controller,
                SpawnPosition = spawnPos,
                Direction = direction,
                InheritedVelocity = inheritedVelocity,
                Damage = _data.damage,
                DamageConfig = null,
                ProjectileConfig = projConfig,
                ProjectilePrefab = _data.projectilePrefab
            };

            V2ProjectileSpawner.Spawn(context);

            if (_data.fireShakeConfig != null)
            {
                V3CameraShakeService.Instance?.TriggerFireShake(spawnPos, direction, _data.fireShakeConfig);
            }
        }

        public IModuleRuntimeData GetRuntimeData() => _data;
    }
}
