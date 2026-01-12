using UnityEngine;

namespace Starfire.Entity.Modules.LifeSupport
{
    public abstract class LifeSupportModuleConfig : ScriptableObject, IModuleConfig
    {
        public ModuleSlotType SlotType => ModuleSlotType.LifeSupport;
        IEntityModule IModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "lifesupport_module";
        [SerializeField] protected string displayName = "Life Support Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Life Support Stats")]
        [SerializeField] protected int crewCapacity = 100;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public int CrewCapacity => Mathf.RoundToInt(crewCapacity * GetTierMultiplier());

        public abstract ILifeSupportModule CreateModule();

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
