using UnityEngine;

namespace Starfire.Entity.Modules.Shield
{
    public abstract class ShieldModuleConfig : ScriptableObject
    {
        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "shield_module";
        [SerializeField] protected string displayName = "Shield Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Shield Stats")]
        [SerializeField] protected int maxShield = 50;
        [SerializeField] protected float regenRate = 5f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public int MaxShield => Mathf.RoundToInt(maxShield * GetTierMultiplier());
        public float RegenRate => regenRate * GetTierMultiplier();

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
