using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject configuration for sensor modules.
    /// </summary>
    [CreateAssetMenu(fileName = "Sensor", menuName = "StarfireV2/Modules/Sensor")]
    public class V2SensorModuleConfig : ScriptableObject, IShipModuleConfig
    {
        [Header("Module Identity")]
        [Tooltip("Unique identifier for this module configuration.")]
        [SerializeField] private string moduleId;

        [Tooltip("Display name shown in UI.")]
        [SerializeField] private string displayName;

        [Header("Sensor Tier")]
        [Tooltip("Module tier affects range and detection capabilities.")]
        [SerializeField] private ModuleTier tier = ModuleTier.Standard;

        [Header("Detection Range")]
        [Tooltip("Configuration for detection ranges at different levels.")]
        [SerializeField] private V2DetectionRangeConfig rangeConfig;

        [Header("Performance")]
        [Tooltip("How often the sensor polls for new contacts (seconds).")]
        [SerializeField] private float pollingInterval = 0.25f;

        [Tooltip("Targeting accuracy multiplier for weapon systems.")]
        [Range(0f, 1f)]
        [SerializeField] private float targetingAccuracy = 0.9f;

        [Header("Filtering")]
        [Tooltip("Configuration for filtering detected entities.")]
        [SerializeField] private V2SensorFilterConfig filterConfig;

        [Header("Threat Detection")]
        [Tooltip("Layers that are considered threats (projectiles, missiles, etc.)")]
        [SerializeField] private LayerMask threatLayers;

        // Public accessors
        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public V2DetectionRangeConfig RangeConfig => rangeConfig ?? V2DetectionRangeConfig.CreateDefault();
        public float PollingInterval => pollingInterval;
        public float TargetingAccuracy => targetingAccuracy;
        public V2SensorFilterConfig FilterConfig => filterConfig;
        public LayerMask ThreatLayers => threatLayers;

        /// <summary>
        /// Effective polling rate accounting for tier bonus.
        /// </summary>
        public float EffectivePollingRate => tier switch
        {
            ModuleTier.Basic => pollingInterval * 1.25f,      // Slower polling
            ModuleTier.Standard => pollingInterval,
            ModuleTier.Advanced => pollingInterval * 0.75f,   // Faster polling
            _ => pollingInterval
        };

        // IShipModuleConfig implementation
        public ShipModuleTypeId TypeId => ShipModuleTypeId.SensorArray;

        public IShipModule CreateModule()
        {
            return new V2SensorModule(this);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(moduleId))
            {
                moduleId = name;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }

            if (pollingInterval <= 0)
            {
                pollingInterval = 0.1f;
            }

            // Ensure range config exists
            if (rangeConfig == null)
            {
                rangeConfig = V2DetectionRangeConfig.CreateDefault();
            }
        }
    }
}
