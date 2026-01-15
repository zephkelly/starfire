using System;
using Starfire.Entity.Modules.Damage;
using UnityEngine;

namespace Starfire.Entity.Modules.Shield
{
    public class BasicShieldModule : IShieldShipModule
    {
        private readonly BasicShieldConfig _config;
        private EntityControllerBase _controller;
        private ShieldState _state = ShieldState.Inactive;
        private float _rechargeDelayTimer;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public int MaxShield => _config.MaxShield;
        public int CurrentShield { get; set; }
        public float RegenRate => _config.RegenRate;
        public float RechargeDelay => _config.RechargeDelay;
        public ShieldState State => _state;
        public DamageResistances Resistances => _config.DamageResistances;

        // Boundary properties
        public bool EnableBoundary => _config.EnableBoundary;
        public Vector2 BoundarySize => _config.BoundarySize;
        public Vector2 BoundaryOffset => _config.BoundaryOffset;
        public int BoundaryResolution => _config.BoundaryResolution;
        public ShieldImpactConfig ShieldImpactConfig => _config.ShieldImpactConfig;
        public ShieldVisualConfig VisualConfig => _config.VisualConfig;

        public event Action OnShieldDestroyed;
        public event Action OnShieldRestored;

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

            switch (_state)
            {
                case ShieldState.RechargeDelay:
                    _rechargeDelayTimer -= deltaTime;
                    if (_rechargeDelayTimer <= 0f)
                    {
                        _state = ShieldState.Recharging;
                    }
                    break;

                case ShieldState.Recharging:
                case ShieldState.Active:
                    if (CurrentShield < MaxShield)
                    {
                        float regenAmount = RegenRate * deltaTime;
                        CurrentShield = Mathf.Min(MaxShield, CurrentShield + Mathf.RoundToInt(regenAmount));

                        if (CurrentShield >= MaxShield)
                        {
                            _state = ShieldState.Active;
                            OnShieldRestored?.Invoke();
                        }
                        else if (_state == ShieldState.Active)
                        {
                            _state = ShieldState.Recharging;
                        }
                    }
                    break;

                case ShieldState.Destroyed:
                    // Shield stays destroyed until externally restored
                    break;
            }
        }

        public float AbsorbDamage(DamageInfo damageInfo)
        {
            // If shield can't absorb, all damage bleeds through
            if (!IsEnabled || _state == ShieldState.Destroyed || _state == ShieldState.Inactive)
            {
                return damageInfo.BaseDamage * damageInfo.ShieldDamageMultiplier;
            }

            // Calculate effective damage with resistances and multipliers
            float resistanceMultiplier = Resistances?.GetDamageMultiplier(damageInfo.Type) ?? 1f;
            float effectiveDamage = damageInfo.BaseDamage * damageInfo.ShieldDamageMultiplier * resistanceMultiplier;

            // Reset recharge delay timer
            _rechargeDelayTimer = RechargeDelay;
            if (_state != ShieldState.Destroyed)
            {
                _state = ShieldState.RechargeDelay;
            }

            // Calculate bleedthrough
            float bleedthrough = 0f;
            if (effectiveDamage >= CurrentShield)
            {
                bleedthrough = effectiveDamage - CurrentShield;
                CurrentShield = 0;
                _state = ShieldState.Destroyed;
                OnShieldDestroyed?.Invoke();
            }
            else
            {
                CurrentShield -= Mathf.RoundToInt(effectiveDamage);
            }

            return bleedthrough;
        }

        /// <summary>
        /// Restore shields to full (e.g., from repair station).
        /// </summary>
        public void RestoreShields()
        {
            CurrentShield = MaxShield;
            _state = ShieldState.Active;
            OnShieldRestored?.Invoke();
        }
    }
}
