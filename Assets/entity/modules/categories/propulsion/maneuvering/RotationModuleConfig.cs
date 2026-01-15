using UnityEngine;

namespace Starfire.Entity.Modules.Rotation
{
    public abstract class RotationModuleConfig : ScriptableObject, IModuleConfig
    {
        public ModuleTypeId TypeId => ModuleTypeId.RotationThruster;
        IEntityModule IModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "rotation_module";
        [SerializeField] protected string displayName = "Rotation Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Base Stats")]
        [SerializeField] protected float rotationSpeed = 180f;

        [Tooltip("Offset in degrees to align sprite's forward direction. -90 = sprite faces up, 0 = sprite faces right")]
        [SerializeField] protected float spriteOffset = -90f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public float RotationSpeed => rotationSpeed;
        public float SpriteOffset => spriteOffset;

        public abstract IRotationShipModule CreateModule();

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
