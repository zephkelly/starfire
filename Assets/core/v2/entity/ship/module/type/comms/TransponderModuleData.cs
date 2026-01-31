using System;
using Starfire.Entity;

namespace StarfireV2
{
    [Serializable]
    public class TransponderModuleData : IModuleRuntimeData
    {
        public string moduleId;
        public string displayName;
        public V2FactionData defaultFaction;
        public ShipClassDefinition shipClass;
        public V2CommChannel defaultActiveChannels = V2CommChannel.AllPublic;
        public V2CommChannel monitoredChannels = V2CommChannel.AllStandard;
        public int defaultCrewComplement;
        public ModuleTier tier = ModuleTier.Standard;

        public string ModuleId => moduleId;
        public ShipModuleTypeId TypeId => ShipModuleTypeId.Transponder;
    }
}
