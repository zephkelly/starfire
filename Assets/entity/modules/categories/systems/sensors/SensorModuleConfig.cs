using UnityEngine;

namespace Starfire.Entity.Modules.Sensor
{
    public abstract class SensorModuleConfig : ScriptableObject, IModuleConfig
    {
        public ModuleTypeId TypeId => ModuleTypeId.SensorArray;
        IEntityModule IModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "sensor_module";
        [SerializeField] protected string displayName = "Sensor Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Sensor Stats")]
        [SerializeField] protected float targetingAccuracy = 1f;

        [Header("Detection Ranges")]
        [SerializeField] protected DetectionRangeConfig rangeConfig = new();

        [Header("Scanning")]
        [Tooltip("How often sensors update detection (seconds)")]
        [SerializeField] protected float pollingInterval = 0.25f;

        [Header("Filtering")]
        [SerializeField] protected SensorFilterConfig filterConfig = new();

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public float DetectionRange => rangeConfig.maxRange * GetTierMultiplier();
        public float TargetingAccuracy => targetingAccuracy * GetTierMultiplier();
        public float PollingInterval => pollingInterval;
        public float EffectivePollingRate => pollingInterval / GetTierMultiplier();
        public DetectionRangeConfig RangeConfig => rangeConfig;
        public SensorFilterConfig FilterConfig => filterConfig;

        public abstract ISensorModule CreateModule();

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
