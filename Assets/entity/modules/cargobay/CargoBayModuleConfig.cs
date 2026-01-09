using UnityEngine;

namespace Starfire.Entity.Modules.CargoBay
{
    public abstract class CargoBayModuleConfig : ScriptableObject
    {
        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "cargobay_module";
        [SerializeField] protected string displayName = "Cargo Bay Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Cargo Bay Stats")]
        [SerializeField] protected int capacity = 100;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public int Capacity => Mathf.RoundToInt(capacity * GetTierMultiplier());

        public abstract ICargoBayModule CreateModule();

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
