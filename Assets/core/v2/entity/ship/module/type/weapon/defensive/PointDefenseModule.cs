using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Implementation of a point defense module that automatically targets and destroys incoming threats.
    /// Typically used to intercept missiles, torpedoes, and other projectiles.
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

        private readonly List<Transform> _trackedTargets = new();
        private Transform _currentTarget;

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

        public void OnAttach(IEntityController controller)
        {
            _controller = controller;
        }

        public void OnDetach()
        {
            OnHardpointUnassigned();
            _controller = null;
            _trackedTargets.Clear();
            _currentTarget = null;
        }

        public void OnUpdate(float deltaTime)
        {
            // Update cooldown
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= deltaTime;
            }

            // Auto-targeting logic
            if (_autoTargetingEnabled && _controller != null)
            {
                // Periodic threat scanning
                _scanTimer -= deltaTime;
                if (_scanTimer <= 0f)
                {
                    ScanForThreats();
                    _scanTimer = _config.ScanInterval;
                }

                // Clean up invalid targets
                CleanupInvalidTargets();

                // Select highest priority target
                SelectPriorityTarget();

                // Auto-fire at current target
                if (_currentTarget != null && CanFire)
                {
                    // Update aim to lead target
                    UpdateAimToTarget();

                    // Fire if aimed
                    if (_visual == null || _visual.IsAimedAtTarget())
                    {
                        Fire();
                    }
                }
            }

            // Update turret visual
            if (_visual != null)
            {
                Vector2 aimDir = _autoTargetingEnabled && _currentTarget != null
                    ? GetLeadDirection(_currentTarget)
                    : _manualAimDirection;

                _visual.SetTargetDirection(aimDir);
            }
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
                if (target == null)
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
            if (_currentTarget == null || !_trackedTargets.Contains(_currentTarget))
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
                if (target == null) continue;

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
            if (_currentTarget == null || _controller == null) return;

            Vector2 leadDir = GetLeadDirection(_currentTarget);

            if (_visual != null)
            {
                _visual.SetTargetDirection(leadDir);
            }
        }

        private Vector2 GetLeadDirection(Transform target)
        {
            if (_controller == null || target == null)
            {
                return _manualAimDirection;
            }

            Vector2 myPosition = GetFirePosition();
            Vector2 targetPosition = target.position;

            // Simple lead calculation if target has rigidbody
            if (_config.TurretSettings != null && _config.TurretSettings.predictTargetPosition)
            {
                var targetRb = target.GetComponent<Rigidbody2D>();
                if (targetRb != null && _config.ProjectileConfig != null)
                {
                    float projectileSpeed = _config.ProjectileConfig.speed;
                    Vector2 targetVelocity = targetRb.linearVelocity;

                    // Calculate intercept point
                    Vector2 toTarget = targetPosition - myPosition;
                    float distance = toTarget.magnitude;
                    float timeToHit = distance / projectileSpeed;

                    // Predict where target will be
                    Vector2 predictedPos = targetPosition + targetVelocity * timeToHit;
                    return (predictedPos - myPosition).normalized;
                }
            }

            return (targetPosition - myPosition).normalized;
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

            if (_visual != null)
            {
                spawnPos = _visual.GetMuzzlePosition();
                direction = _visual.GetMuzzleDirection();
            }
            else if (_hardpoint != null)
            {
                spawnPos = _hardpoint.MountPoint.position;
                direction = _currentTarget != null
                    ? GetLeadDirection(_currentTarget)
                    : _hardpoint.WorldFiringDirection;
            }
            else if (_controller != null)
            {
                spawnPos = _controller.Transform.position;
                direction = _currentTarget != null
                    ? GetLeadDirection(_currentTarget)
                    : _manualAimDirection;
            }
            else
            {
                Debug.LogWarning($"Cannot spawn projectile for '{_config.DisplayName}': no position reference available");
                return;
            }

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
        }
    }
}
