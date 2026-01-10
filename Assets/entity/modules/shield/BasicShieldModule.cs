using UnityEngine;

namespace Starfire.Entity.Modules.Shield
{
    public class BasicShieldModule : IShieldModule
    {
        private readonly BasicShieldConfig _config;
        private EntityControllerBase _controller;
        private ShieldState _state = ShieldState.Inactive;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public int MaxShield => _config.MaxShield;
        public int CurrentShield { get; set; }
        public float RegenRate => _config.RegenRate;
        public ShieldState State => _state;

        public BasicShieldModule(BasicShieldConfig config)
        {
            _config = config;
            CurrentShield = MaxShield;
            _state = ShieldState.Active;
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
            if (!IsEnabled) return;

            if (_state == ShieldState.Active && CurrentShield < MaxShield)
            {
                CurrentShield = Mathf.Min(MaxShield, CurrentShield + Mathf.RoundToInt(RegenRate * deltaTime));
            }
            else if (_state == ShieldState.Destroyed && CurrentShield >= MaxShield)
            {
                _state = ShieldState.Active;
            }
        }

        public void TakeDamage(int amount)
        {
            if (!IsEnabled || _state == ShieldState.Destroyed) return;

            CurrentShield = Mathf.Max(0, CurrentShield - amount);

            if (CurrentShield <= 0)
            {
                _state = ShieldState.Destroyed;
            }
        }
    }
}
