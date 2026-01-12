using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    public abstract class WeaponModuleConfig : ScriptableObject, IModuleConfig
    {
        public ModuleSlotType SlotType => ModuleSlotType.Weapon;
        IEntityModule IModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "weapon_module";
        [SerializeField] protected string displayName = "Weapon Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Weapon Stats")]
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float fireRate = 1f;
        [SerializeField] protected float range = 20f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public float Damage => damage * GetTierMultiplier();
        public float FireRate => fireRate * GetTierMultiplier();
        public float Range => range;

        public abstract IWeaponModule CreateModule();

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
