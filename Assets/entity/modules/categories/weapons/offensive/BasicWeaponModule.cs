using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    public class BasicWeaponModule : IWeaponModule
    {
        private readonly BasicWeaponConfig _config;
        private EntityControllerBase _controller;

        // Runtime state
        private float _cooldownTimer;
        private WeaponVisual _visual;
        private HardpointMarker _hardpoint;
        private Vector2 _aimDirection;

        // IEntityModule
        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        // IWeaponModule stats
        public float Damage => _config.Damage;
        public float FireRate => _config.FireRate;
        public float Range => _config.Range;

        // IWeaponModule state
        public bool IsTurret => _config.TurretSettings?.isTurret ?? false;

        public bool CanFire
        {
            get
            {
                if (!IsEnabled) return false;
                if (_cooldownTimer > 0f) return false;

                // If no visual/hardpoint, we can still fire (for non-visual weapons)
                // But if we have a turret, check if it's aimed correctly
                if (_visual != null && IsTurret)
                {
                    if (!_config.TurretSettings.canFireWhileRotating && !_visual.IsAimedAtTarget())
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public BasicWeaponModule(BasicWeaponConfig config)
        {
            _config = config;
        }

        public void OnAttach(EntityControllerBase controller)
        {
            _controller = controller;
        }

        public void OnHardpointAssigned(HardpointMarker hardpoint)
        {
            _hardpoint = hardpoint;

            if (_hardpoint != null && _config.WeaponVisualPrefab != null)
            {
                // Instantiate visual at hardpoint
                var visualGO = Object.Instantiate(
                    _config.WeaponVisualPrefab,
                    _hardpoint.MountPoint
                );
                visualGO.transform.localPosition = Vector3.zero;
                visualGO.transform.localRotation = Quaternion.identity;

                _visual = visualGO.GetComponent<WeaponVisual>();
                if (_visual != null)
                {
                    _visual.Initialize(_config.TurretSettings);
                }
                else
                {
                    Debug.LogWarning($"Weapon visual prefab '{_config.WeaponVisualPrefab.name}' is missing WeaponVisual component");
                }
            }
        }

        public void OnDetach()
        {
            // Cleanup visual
            if (_visual != null)
            {
                Object.Destroy(_visual.gameObject);
                _visual = null;
            }

            _controller = null;
            _hardpoint = null;
        }

        public void OnUpdate(float deltaTime)
        {
            // Cooldown
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= deltaTime;
            }

            // Update turret aim
            if (_visual != null && IsTurret)
            {
                _visual.SetTargetDirection(_aimDirection);
            }
        }

        public void SetAimDirection(Vector2 worldDirection)
        {
            _aimDirection = worldDirection.normalized;
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

        private void SpawnProjectile()
        {
            var projConfig = _config.ProjectileConfig;

            // Physics mode requires a prefab
            if (projConfig.projectileMode == ProjectileMode.Physics && _config.ProjectilePrefab == null)
            {
                Debug.LogWarning($"Weapon '{DisplayName}' has no projectile prefab assigned for Physics mode");
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
                // Fallback: spawn from controller position, fire forward
                spawnPos = _controller.transform.position;
                direction = _controller.transform.up;
            }
            else
            {
                Debug.LogWarning($"Cannot spawn projectile for '{DisplayName}': no position reference available");
                return;
            }

            // Calculate inherited velocity if enabled
            Vector2 inheritedVelocity = Vector2.zero;
            if (projConfig.inheritVelocity && _controller?.Rigidbody != null)
            {
                inheritedVelocity = _controller.Rigidbody.linearVelocity;
            }

            // Build spawn context and delegate to spawner
            var context = new ProjectileSpawnContext
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

            ProjectileSpawner.Spawn(context);
        }
    }
}
