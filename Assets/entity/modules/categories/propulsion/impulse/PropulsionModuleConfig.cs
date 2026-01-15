using UnityEngine;

namespace Starfire.Entity.Modules.Propulsion
{
    public abstract class PropulsionModuleConfig : ScriptableObject, IModuleConfig
    {
        public ModuleTypeId TypeId => ModuleTypeId.ImpulseEngine;
        IEntityModule IModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "propulsion_module";
        [SerializeField] protected string displayName = "Propulsion Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Propulsion Stats")]
        [SerializeField] protected float maxSpeed = 10f;
        [SerializeField] protected float acceleration = 5f;
        [SerializeField] protected float drag = 1f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public float MaxSpeed => maxSpeed * GetTierMultiplier();
        public float Acceleration => acceleration * GetTierMultiplier();
        public float Drag => drag;

        public abstract IPropulsionModule CreateModule();

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
