using System;
using UnityEngine;

namespace StarfireV2
{
    public class ShieldModule : IShipShieldModule
    {
        private readonly ShieldModuleData _data;
        private readonly DamageResistances _resistances;
        private readonly ShieldImpactConfig _impactConfig;
        private readonly ShieldVisualConfig _visualConfig;

        private IEntityController _controller;
        private ShieldState _state = ShieldState.Active;
        private float _rechargeDelayTimer;

        // Runtime GameObjects created on attach
        private GameObject _boundaryObject;
        private GameObject _visualObject;
        private ShieldBoundary _boundary;
        private ShieldVisual _visual;

        // IShipModule
        public ShipModuleCategory Category => ShipModuleCategory.Defense;
        public ShipModuleType Type => ShipModuleType.Shield;

        // IEntityModule
        public string ModuleId => _data.moduleId;
        public bool IsEnabled { get; set; } = true;

        // IShipShieldModule
        public int MaxShield => _data.maxShield;
        public int CurrentShield { get; set; }
        public float RegenRate => _data.regenRate;
        public float RechargeDelay => _data.rechargeDelay;
        public ShieldState State => _state;
        public DamageResistances Resistances => _resistances;

        public bool EnableBoundary => _data.enableBoundary;
        public Vector2 BoundarySize => _data.boundarySize;
        public Vector2 BoundaryOffset => _data.boundaryOffset;
        public int BoundaryResolution => _data.boundaryResolution;

        public ShieldImpactConfig ShieldImpactConfig => _impactConfig;
        public ShieldVisualConfig VisualConfig => _visualConfig;

        public event Action OnShieldDestroyed;
        public event Action OnShieldRestored;

        public ShieldModule(ShieldModuleData data, DamageResistances resistances = null,
            ShieldImpactConfig impactConfig = null, ShieldVisualConfig visualConfig = null)
        {
            _data = data;
            _resistances = resistances;
            _impactConfig = impactConfig;
            _visualConfig = visualConfig;
            CurrentShield = MaxShield;
        }

        public void OnAttach(IEntityController controller)
        {
            _controller = controller;
            _state = ShieldState.Active;

            CreateVisualComponents();
        }

        public void OnDetach()
        {
            DestroyVisualComponents();
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
                    break;
            }
        }

        public float AbsorbDamage(V2DamageInfo damageInfo)
        {
            if (!IsEnabled || _state == ShieldState.Destroyed || _state == ShieldState.Inactive)
            {
                return damageInfo.CalculateShieldDamage();
            }

            var config = damageInfo.DamageConfig ?? V2WeaponDamageConfig.Default;
            float resistanceMultiplier = _resistances != null ? _resistances.GetDamageMultiplier(config.damageType) : 1f;
            float effectiveDamage = damageInfo.CalculateShieldDamage() * resistanceMultiplier;

            _rechargeDelayTimer = RechargeDelay;
            if (_state != ShieldState.Destroyed)
            {
                _state = ShieldState.RechargeDelay;
            }

            if (effectiveDamage >= CurrentShield)
            {
                float bleedthrough = effectiveDamage - CurrentShield;
                CurrentShield = 0;
                _state = ShieldState.Destroyed;
                OnShieldDestroyed?.Invoke();
                return bleedthrough;
            }

            CurrentShield -= Mathf.RoundToInt(effectiveDamage);
            return 0f;
        }

        public void RestoreShields()
        {
            CurrentShield = MaxShield;
            _state = ShieldState.Active;
            OnShieldRestored?.Invoke();
        }

        private void CreateVisualComponents()
        {
            if (_controller == null) return;
            var parent = _controller.Transform;

            if (_visualConfig != null)
            {
                _visualObject = new GameObject("ShieldVisual");
                _visualObject.transform.SetParent(parent, false);
                _visualObject.AddComponent<MeshFilter>();
                _visualObject.AddComponent<MeshRenderer>();
                _visual = _visualObject.AddComponent<ShieldVisual>();
                _visual.Initialize(this, _controller);
            }

            if (EnableBoundary)
            {
                _boundaryObject = new GameObject("ShieldBoundary");
                _boundaryObject.transform.SetParent(parent, false);
                _boundaryObject.AddComponent<PolygonCollider2D>();
                _boundary = _boundaryObject.AddComponent<ShieldBoundary>();
                _boundary.Initialize(this, _controller, _visual);
            }
        }

        private void DestroyVisualComponents()
        {
            if (_boundaryObject != null)
            {
                UnityEngine.Object.Destroy(_boundaryObject);
                _boundaryObject = null;
                _boundary = null;
            }

            if (_visualObject != null)
            {
                UnityEngine.Object.Destroy(_visualObject);
                _visualObject = null;
                _visual = null;
            }
        }
    }
}
