using UnityEngine;

namespace Starfire.Entity.Modules.WarpDrive
{
    public abstract class WarpDriveModuleConfig : ScriptableObject
    {
        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "warpdrive_module";
        [SerializeField] protected string displayName = "Warp Drive Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Warp Drive Stats")]
        [SerializeField] protected float warpSpeed = 50f;
        [SerializeField] protected float chargeTime = 2f;
        [SerializeField] protected float cooldown = 5f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public float WarpSpeed => warpSpeed * GetTierMultiplier();
        public float ChargeTime => chargeTime / GetTierMultiplier();
        public float Cooldown => cooldown / GetTierMultiplier();

        public abstract IWarpDriveModule CreateModule();

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
