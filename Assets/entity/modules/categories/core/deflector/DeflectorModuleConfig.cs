using UnityEngine;

namespace Starfire.Entity.Modules.Deflector
{
    public abstract class DeflectorModuleConfig : ScriptableObject, IModuleConfig
    {
        public ModuleTypeId TypeId => ModuleTypeId.Deflector;
        IEntityModule IModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "deflector_module";
        [SerializeField] protected string displayName = "Deflector Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Deflector Stats")]
        [SerializeField] protected float deflectionStrength = 1f;
        [SerializeField] protected float beamFocusMultiplier = 1f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public float DeflectionStrength => deflectionStrength * GetTierMultiplier();
        public float BeamFocusMultiplier => beamFocusMultiplier * GetTierMultiplier();

        public abstract IDeflectorShipModule CreateModule();

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
