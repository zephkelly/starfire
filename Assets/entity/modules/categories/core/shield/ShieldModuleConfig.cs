using Starfire.Entity.Modules.Damage;
using UnityEngine;

namespace Starfire.Entity.Modules.Shield
{
    public abstract class ShieldModuleConfig : ScriptableObject, IModuleConfig
    {
        public ModuleTypeId TypeId => ModuleTypeId.Shield;
        IEntityModule IModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "shield_module";
        [SerializeField] protected string displayName = "Shield Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Shield Stats")]
        [SerializeField] protected int maxShield = 50;
        [SerializeField] protected float regenRate = 5f;

        [Header("Recharge Settings")]
        [Tooltip("Seconds to wait after taking damage before regeneration starts")]
        [SerializeField] protected float rechargeDelay = 3f;

        [Header("Damage Resistances")]
        [Tooltip("Optional: Per-damage-type resistances for this shield")]
        [SerializeField] protected DamageResistances damageResistances;

        [Header("Shield Boundary")]
        [Tooltip("Enable a physical boundary collider for the shield")]
        [SerializeField] protected bool enableBoundary = true;

        [Tooltip("Size of the elliptical boundary (x = horizontal radius, y = vertical radius)")]
        [SerializeField] protected Vector2 boundarySize = new Vector2(2f, 1.5f);

        [Tooltip("Offset of the boundary center from the entity origin")]
        [SerializeField] protected Vector2 boundaryOffset = Vector2.zero;

        [Tooltip("Number of vertices used to approximate the ellipse (higher = smoother)")]
        [SerializeField, Range(8, 64)] protected int boundaryResolution = 24;

        [Tooltip("Impact effect configuration for shield hits")]
        [SerializeField] protected ShieldImpactConfig shieldImpactConfig;

        [Header("Shield Visual")]
        [Tooltip("Visual appearance configuration for the shield barrier")]
        [SerializeField] protected ShieldVisualConfig visualConfig;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public int MaxShield => Mathf.RoundToInt(maxShield * GetTierMultiplier());
        public float RegenRate => regenRate * GetTierMultiplier();
        public float RechargeDelay => rechargeDelay;
        public DamageResistances DamageResistances => damageResistances;
        public bool EnableBoundary => enableBoundary;
        public Vector2 BoundarySize => boundarySize;
        public Vector2 BoundaryOffset => boundaryOffset;
        public int BoundaryResolution => boundaryResolution;
        public ShieldImpactConfig ShieldImpactConfig => shieldImpactConfig;
        public ShieldVisualConfig VisualConfig => visualConfig;

        public abstract IShieldModule CreateModule();

        protected float GetTierMultiplier()
        {
            return tier switch
            {
                ModuleTier.Basic => 0.75f,
                ModuleTier.Standard => 1.0f,
                ModuleTier.Advanced => 1.25f,
                _ => 1.0f
            };
        }
    }
}
