using UnityEngine;

namespace Starfire.Entity.Modules.Hull
{
    public class BasicHullModule : IHullModule
    {
        private readonly BasicHullConfig _config;
        private EntityControllerBase _controller;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public int MaxHealth => _config.MaxHealth;
        public int CurrentHealth { get; set; }
        public float DamageResistance => _config.DamageResistance;

        public BasicHullModule(BasicHullConfig config)
        {
            _config = config;
            CurrentHealth = MaxHealth;
        }

        public void OnAttach(EntityControllerBase controller)
        {
            _controller = controller;
        }

        public void OnDetach()
        {
            _controller = null;
        }

        public void OnUpdate(float deltaTime) { }

        public void TakeDamage(int amount)
        {
            if (!IsEnabled) return;

            int reducedDamage = Mathf.RoundToInt(amount * (1f - DamageResistance));
            CurrentHealth = Mathf.Max(0, CurrentHealth - reducedDamage);
        }
    }
}
