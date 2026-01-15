using UnityEngine;

namespace Starfire.Entity.Modules.WarpDrive
{
    public class BasicWarpDriveModule : IWarpDriveShipModule
    {
        private readonly BasicWarpDriveConfig _config;
        private EntityControllerBase _controller;
        private float _cooldownTimer;
        private bool _isWarping;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public float WarpSpeed => _config.WarpSpeed;
        public float ChargeTime => _config.ChargeTime;
        public float Cooldown => _config.Cooldown;
        public bool IsReady => _cooldownTimer <= 0f && !_isWarping;

        public BasicWarpDriveModule(BasicWarpDriveConfig config)
        {
            _config = config;
        }

        public void OnAttach(EntityControllerBase controller)
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

        public void EngageWarp()
        {
            if (!IsEnabled || !IsReady) return;
            _isWarping = true;
        }

        public void DisengageWarp()
        {
            if (!_isWarping) return;
            _isWarping = false;
            _cooldownTimer = Cooldown;
        }
    }
}
