using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    public class BasicWeaponModule : IWeaponModule
    {
        private readonly BasicWeaponConfig _config;
        private EntityController _controller;
        private float _cooldownTimer;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public float Damage => _config.Damage;
        public float FireRate => _config.FireRate;
        public float Range => _config.Range;

        public BasicWeaponModule(BasicWeaponConfig config)
        {
            _config = config;
        }

        public void OnAttach(EntityController controller)
        {
            _controller = controller;
        }

        public void OnDetach()
        {
            _controller = null;
        }

        public void OnUpdate(float deltaTime)
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= deltaTime;
            }
        }

        public void Fire()
        {
            if (!IsEnabled || _cooldownTimer > 0f) return;
            _cooldownTimer = 1f / FireRate;
        }
    }
}
