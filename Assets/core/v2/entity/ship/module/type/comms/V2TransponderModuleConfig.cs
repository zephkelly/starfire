using Starfire.Entity;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject configuration for transponder modules.
    /// </summary>
    [CreateAssetMenu(fileName = "Transponder", menuName = "StarfireV2/Modules/Transponder")]
    public class V2TransponderModuleConfig : ScriptableObject, IShipModuleConfig
    {
        [Header("Module Identity")]
        [Tooltip("Unique identifier for this module configuration.")]
        [SerializeField] private string moduleId;

        [Tooltip("Display name shown in UI.")]
        [SerializeField] private string displayName;

        [Header("Faction & Class")]
        [Tooltip("Default faction for ships using this transponder.")]
        [SerializeField] private V2FactionData defaultFaction;

        [Tooltip("Ship class definition. Can be overridden per-instance.")]
        [SerializeField] private ShipClassDefinition shipClass;

        [Header("Communication Channels")]
        [Tooltip("Channels that are active by default when the transponder is created.")]
        [SerializeField] private V2CommChannel defaultActiveChannels = V2CommChannel.AllPublic;

        [Tooltip("Channels that this transponder monitors for incoming messages.")]
        [SerializeField] private V2CommChannel monitoredChannels = V2CommChannel.AllStandard;

        [Header("Crew")]
        [Tooltip("Default crew complement for ships using this transponder.")]
        [SerializeField] private int defaultCrewComplement = 0;

        [Header("Signal")]
        [Tooltip("Module tier affects signal strength.")]
        [SerializeField] private ModuleTier tier = ModuleTier.Standard;

        // Public accessors
        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public V2FactionData DefaultFaction => defaultFaction;
        public ShipClassDefinition ShipClass => shipClass;
        public V2CommChannel DefaultActiveChannels => defaultActiveChannels;
        public V2CommChannel MonitoredChannels => monitoredChannels;
        public int DefaultCrewComplement => defaultCrewComplement;
        public ModuleTier Tier => tier;

        /// <summary>
        /// Signal strength multiplier based on module tier.
        /// </summary>
        public float SignalStrengthMultiplier => tier switch
        {
            ModuleTier.Basic => 0.75f,
            ModuleTier.Standard => 1.0f,
            ModuleTier.Advanced => 1.25f,
            _ => 1.0f
        };

        // IShipModuleConfig implementation
        public ShipModuleTypeId TypeId => ShipModuleTypeId.Transponder;

        public IShipModule CreateModule()
        {
            return new V2TransponderModule(this);
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

            if (defaultCrewComplement < 0)
            {
                defaultCrewComplement = 0;
            }
        }
    }
}
