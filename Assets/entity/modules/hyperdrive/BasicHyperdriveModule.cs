using UnityEngine;

namespace Starfire.Entity.Modules.Hyperdrive
{
    public class BasicHyperdriveModule : IHyperdriveModule
    {
        private readonly BasicHyperdriveConfig _config;
        private EntityControllerBase _controller;
        private float _chargeTimer;
        private bool _isCharging;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public float HyperdriveRange => _config.HyperdriveRange;
        public float ChargeTime => _config.ChargeTime;
        public bool IsReady => !_isCharging && _chargeTimer <= 0f;

        public BasicHyperdriveModule(BasicHyperdriveConfig config)
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
            if (_isCharging)
            {
                _chargeTimer -= deltaTime;
                if (_chargeTimer <= 0f)
                {
                    _isCharging = false;
                }
            }
        }

        public void Jump(Vector2 destination)
        {
            if (!IsEnabled || !IsReady) return;

            _isCharging = true;
            _chargeTimer = ChargeTime;
        }
    }
}
