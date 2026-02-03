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
        private readonly OffensiveWeaponModuleData _data;
        private IEntityController _controller;
        private V2HardpointMarker _hardpoint;
        private V2WeaponVisual _visual;
        private float _cooldownTimer;
        private Vector2 _aimDirection = Vector2.up;

        // IEntityModule
        public string ModuleId => _data.moduleId;
        public bool IsEnabled { get; set; } = true;

        // IShipModule
        public ShipModuleCategory Category => ShipModuleCategory.Offense;
        public ShipModuleType Type => ShipModuleType.Weapon;

        // IWeaponModule
        public WeaponWeightClass WeightClass => _data.weightClass;
        public float FireRate => _data.fireRate;
        public float Range => _data.range;
        public bool IsTurret => _data.turretSettings?.isTurret ?? false;
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
                    var turretSettings = _data.turretSettings;
                    if (turretSettings != null && !turretSettings.canFireWhileRotating && !_visual.IsAimedAtTarget())
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // IOffensiveWeaponModule
        public float Damage => _data.damage;
        public V2WeaponDamageConfig DamageConfig => _data.damageConfig ?? V2WeaponDamageConfig.Default;
        public V2ProjectileConfig ProjectileConfig => _data.projectileConfig;

        public OffensiveWeaponModule(OffensiveWeaponModuleData data)
        {
            _data = data;
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
                Debug.LogWarning($"Cannot mount {WeightClass} weapon '{_data.displayName}' on {hardpoint.WeightClass} hardpoint '{hardpoint.SlotId}'");
                return;
            }

            _hardpoint = hardpoint;

            // Instantiate visual if we have a prefab
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
                return false;
            }

            // Set cooldown
            _cooldownTimer = 1f / FireRate;

            // Spawn projectile
            SpawnProjectile();

            return true;
        }

        private void SpawnProjectile()
        {
            var projConfig = _data.projectileConfig;
            if (projConfig == null)
            {
                Debug.LogWarning($"[V2Weapon] '{_data.displayName}' has no ProjectileConfig assigned");
                return;
            }

            if (projConfig.mode == V2ProjectileMode.Physics && _data.projectilePrefab == null)
            {
                Debug.LogWarning($"[V2Weapon] '{_data.displayName}' has no projectile prefab for Physics mode");
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
                direction = _hardpoint.WorldFiringDirection;
            }
            else if (_controller != null)
            {
                // Fallback: spawn from controller position, fire in aim direction
                spawnPos = _controller.Transform.position;
                direction = _aimDirection;
            }
            else
            {
                Debug.LogWarning($"[V2Weapon] Cannot spawn projectile for '{_data.displayName}': no position reference available");
                return;
            }

            // Override direction for mouse targeting mode
            var turretSettings = _data.turretSettings;
            if (turretSettings != null && turretSettings.targetingMode == V2TargetingMode.MouseCursor)
            {
                direction = _aimDirection;
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
                DamageConfig = _data.damageConfig,
                ProjectileConfig = projConfig,
                ProjectilePrefab = _data.projectilePrefab
            };

            V2ProjectileSpawner.Spawn(context);

            // Trigger fire shake (recoil)
            if (_data.fireShakeConfig != null)
            {
                V3CameraShakeService.Instance?.TriggerFireShake(spawnPos, direction, _data.fireShakeConfig);
            }
        }

        public IModuleRuntimeData GetRuntimeData() => _data;
    }
}
