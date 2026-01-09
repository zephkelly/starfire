using UnityEngine;

namespace Starfire.Entity.Modules.Sensor
{
    public abstract class SensorModuleConfig : ScriptableObject
    {
        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "sensor_module";
        [SerializeField] protected string displayName = "Sensor Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Sensor Stats")]
        [SerializeField] protected float detectionRange = 50f;
        [SerializeField] protected float targetingAccuracy = 1f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public float DetectionRange => detectionRange * GetTierMultiplier();
        public float TargetingAccuracy => targetingAccuracy * GetTierMultiplier();

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
