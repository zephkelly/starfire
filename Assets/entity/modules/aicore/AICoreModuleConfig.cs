using UnityEngine;

namespace Starfire.Entity.Modules.AICore
{
    public abstract class AICoreModuleConfig : ScriptableObject
    {
        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "aicore_module";
        [SerializeField] protected string displayName = "AI Core Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("AI Core Stats")]
        [SerializeField] protected float processingPower = 1f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public float ProcessingPower => processingPower * GetTierMultiplier();

        public abstract IAICoreModule CreateModule();

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
