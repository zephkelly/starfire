using System;
using Starfire.Entity.Modules.Damage;
using UnityEngine;

namespace Starfire.Entity.Modules.Hull
{
    public class BasicHullModule : IHullShipModule
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
        public DamageResistances TypeResistances => _config.DamageResistances;
        public bool IsDestroyed => CurrentHealth <= 0;

        public event Action OnHullDestroyed;
        public event Action<float> OnHullDamaged;

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

        public float TakeDamage(DamageInfo damageInfo)
        {
            if (!IsEnabled || IsDestroyed) return 0f;

            // Calculate effective damage with type resistance and flat resistance
            float typeResistanceMultiplier = TypeResistances?.GetDamageMultiplier(damageInfo.Type) ?? 1f;
            float flatResistanceMultiplier = 1f - DamageResistance;
            float effectiveDamage = damageInfo.BaseDamage * damageInfo.HullDamageMultiplier * typeResistanceMultiplier * flatResistanceMultiplier;

            int damageInt = Mathf.RoundToInt(effectiveDamage);
            CurrentHealth = Mathf.Max(0, CurrentHealth - damageInt);

            OnHullDamaged?.Invoke(effectiveDamage);

            if (CurrentHealth <= 0)
            {
                OnHullDestroyed?.Invoke();
            }

            return effectiveDamage;
        }

        /// <summary>
        /// Repair hull by a specified amount.
        /// </summary>
        public void Repair(int amount)
        {
            if (!IsEnabled) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        }
    }
}
