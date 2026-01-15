using Starfire.Entity.Modules.Damage;
using UnityEngine;

namespace Starfire.Entity.Modules.Hull
{
    public abstract class HullModuleConfig : ScriptableObject, IModuleConfig
    {
        public ModuleTypeId TypeId => ModuleTypeId.Hull;
        IEntityModule IModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "hull_module";
        [SerializeField] protected string displayName = "Hull Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Hull Stats")]
        [SerializeField] protected int maxHealth = 100;
        [SerializeField] protected float damageResistance = 0f;

        [Header("Damage Resistances")]
        [Tooltip("Optional: Per-damage-type resistances for this hull")]
        [SerializeField] protected DamageResistances damageResistances;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public int MaxHealth => Mathf.RoundToInt(maxHealth * GetTierMultiplier());
        public float DamageResistance => damageResistance;
        public DamageResistances DamageResistances => damageResistances;

        public abstract IHullShipModule CreateModule();

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
