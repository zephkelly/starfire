using UnityEngine;

namespace Starfire.Entity.Modules.Transponder
{
    public abstract class TransponderModuleConfig : ScriptableObject, IModuleConfig
    {
        public ModuleTypeId TypeId => ModuleTypeId.Transponder;
        IEntityModule IModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "transponder_module";
        [SerializeField] protected string displayName = "Transponder Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Faction")]
        [SerializeField] protected FactionData defaultFaction;

        [Header("Ship Reference")]
        [Tooltip("Optional - can be overridden by ShipController at runtime")]
        [SerializeField] protected ShipClassDefinition shipClass;

        [Header("Channels")]
        [SerializeField] protected CommChannel defaultActiveChannels = CommChannel.AllPublic;
        [SerializeField] protected CommChannel monitoredChannels = CommChannel.AllStandard;

        [Header("Crew")]
        [SerializeField] protected int defaultCrewComplement = 0;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public FactionData DefaultFaction => defaultFaction;
        public ShipClassDefinition ShipClass => shipClass;
        public CommChannel DefaultActiveChannels => defaultActiveChannels;
        public CommChannel MonitoredChannels => monitoredChannels;
        public int DefaultCrewComplement => defaultCrewComplement;

        public float SignalStrengthMultiplier => GetTierMultiplier();

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

        public abstract ITransponderModule CreateModule();
    }
}
