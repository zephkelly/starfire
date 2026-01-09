using UnityEngine;

namespace Starfire.Entity.Modules.Hyperdrive
{
    public abstract class HyperdriveModuleConfig : ScriptableObject
    {
        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "hyperdrive_module";
        [SerializeField] protected string displayName = "Hyperdrive Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Hyperdrive Stats")]
        [SerializeField] protected float hyperdriveRange = 1000f;
        [SerializeField] protected float chargeTime = 5f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public float HyperdriveRange => hyperdriveRange * GetTierMultiplier();
        public float ChargeTime => chargeTime / GetTierMultiplier();

        public abstract IHyperdriveModule CreateModule();

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
