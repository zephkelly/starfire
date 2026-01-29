using Starfire.Core.V3.Cam.Effects;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Implementation of an offensive weapon module that fires projectiles at targets.
    /// Supports fixed and turret-mounted configurations.
    /// </summary>
    public class OffensiveWeaponModule : IOffensiveWeaponModule
    {
        private readonly OffensiveWeaponModuleConfig _config;
        private IEntityController _controller;
        private V2HardpointMarker _hardpoint;
        private V2WeaponVisual _visual;
        private float _cooldownTimer;
        private Vector2 _aimDirection = Vector2.up;

        // IEntityModule
        public string ModuleId => _config.ModuleId;
        public bool IsEnabled { get; set; } = true;

        // IShipModule
        public ShipModuleCategory Category => ShipModuleCategory.Offense;
        public ShipModuleType Type => ShipModuleType.Weapon;

        // IWeaponModule
        public WeaponWeightClass WeightClass => _config.WeightClass;
        public float FireRate => _config.FireRate;
        public float Range => _config.Range;
        public bool IsTurret => _config.TurretSettings?.isTurret ?? false;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

        public bool CanFire
        {
            get
            {
                if (!IsEnabled) return false;
                if (_cooldownTimer > 0f) return false;

                // For turrets, check if aimed at target
                if (_visual != null && IsTurret)
                {
                    var turretSettings = _config.TurretSettings;
                    if (turretSettings != null && !turretSettings.canFireWhileRotating && !_visual.IsAimedAtTarget())
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // IOffensiveWeaponModule
        public float Damage => _config.Damage;
        public V2WeaponDamageConfig DamageConfig => _config.DamageConfig;
        public V2ProjectileConfig ProjectileConfig => _config.ProjectileConfig;

        public OffensiveWeaponModule(OffensiveWeaponModuleConfig config)
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
        }

        public void OnUpdate(float deltaTime)
        {
            // Update cooldown
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= deltaTime;
            }

            // Calculate mouse targeting direction (works for both turret and non-turret)
            var turretSettings = _config.TurretSettings;
            if (turretSettings != null && turretSettings.targetingMode == V2TargetingMode.MouseCursor)
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    // Get weapon position from visual, hardpoint, or controller
                    Vector2 weaponPos = _visual != null
                        ? (Vector2)_visual.transform.position
                        : (_hardpoint != null
                            ? (Vector2)_hardpoint.MountPoint.position
                            : (_controller != null ? (Vector2)_controller.Transform.position : Vector2.zero));

                    Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    Vector2 toMouse = mouseWorld - weaponPos;

                    if (toMouse.sqrMagnitude > 0.001f)
                    {
                        _aimDirection = toMouse.normalized;
                    }
                }
            }

            // Update turret visual rotation (if turret)
            if (_visual != null && IsTurret)
            {
                _visual.SetTargetDirection(_aimDirection);
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
            // Clean up visual
            if (_visual != null)
            {
                Object.Destroy(_visual.gameObject);
                _visual = null;
            }

            _hardpoint = null;
        }

        // Explicit interface implementation for IShipOffensiveModule.OnHardpointAssigned()
        // V2 weapons use OnHardpointAssigned(V2HardpointMarker) instead
        void IShipOffensiveModule.OnHardpointAssigned()
        {
            // No-op: hardpoint assignment for v2 weapons requires the V2HardpointMarker parameter
        }

        public void SetAimDirection(Vector2 worldDirection)
        {
            if (worldDirection.sqrMagnitude > 0.001f)
            {
                _aimDirection = worldDirection.normalized;
            }
        }

        public bool Fire()
        {
            if (!CanFire)
            {
                Debug.Log($"[V2Weapon] Fire failed: CanFire=false (IsEnabled={IsEnabled}, Cooldown={_cooldownTimer:F2}, IsTurret={IsTurret})");
                return false;
            }

            Debug.Log($"[V2Weapon] Firing '{_config.DisplayName}'");

            // Set cooldown
            _cooldownTimer = 1f / FireRate;

            // Spawn projectile
            SpawnProjectile();

            return true;
        }

        private void SpawnProjectile()
        {
            var projConfig = _config.ProjectileConfig;
            if (projConfig == null)
            {
                Debug.LogWarning($"[V2Weapon] '{_config.DisplayName}' has no ProjectileConfig assigned");
                return;
            }

            Debug.Log($"[V2Weapon] ProjectileConfig mode: {projConfig.mode}");

            // Physics mode requires a prefab
            if (projConfig.mode == V2ProjectileMode.Physics && _config.ProjectilePrefab == null)
            {
                Debug.LogWarning($"[V2Weapon] '{_config.DisplayName}' has no projectile prefab for Physics mode");
                return;
            }

            // Determine spawn position and direction
            Vector2 spawnPos;
            Vector2 direction;

            if (_visual != null)
            {
                spawnPos = _visual.GetMuzzlePosition();
                direction = _visual.GetMuzzleDirection();
                Debug.Log($"[V2Weapon] Using visual: pos={spawnPos}, dir={direction}");
            }
            else if (_hardpoint != null)
            {
                spawnPos = _hardpoint.MountPoint.position;
                direction = _hardpoint.WorldFiringDirection;
                Debug.Log($"[V2Weapon] Using hardpoint: pos={spawnPos}, dir={direction}");
            }
            else if (_controller != null)
            {
                // Fallback: spawn from controller position, fire in aim direction
                spawnPos = _controller.Transform.position;
                direction = _aimDirection;
                Debug.Log($"[V2Weapon] Using controller fallback: pos={spawnPos}, dir={direction}");
            }
            else
            {
                Debug.LogWarning($"[V2Weapon] Cannot spawn projectile for '{_config.DisplayName}': no position reference available");
                return;
            }

            // Override direction for mouse targeting mode
            var turretSettings = _config.TurretSettings;
            if (turretSettings != null && turretSettings.targetingMode == V2TargetingMode.MouseCursor)
            {
                direction = _aimDirection;
                Debug.Log($"[V2Weapon] Mouse targeting override: dir={direction}");
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
                Damage = Damage,
                DamageConfig = _config.DamageConfig,
                ProjectileConfig = projConfig,
                ProjectilePrefab = _config.ProjectilePrefab
            };

            Debug.Log($"[V2Weapon] Calling V2ProjectileSpawner.Spawn() at {spawnPos}");
            V2ProjectileSpawner.Spawn(context);

            // Trigger fire shake (recoil)
            if (_config.FireShakeConfig != null)
            {
                V3CameraShakeService.Instance?.TriggerFireShake(spawnPos, direction, _config.FireShakeConfig);
            }
        }
    }
}
