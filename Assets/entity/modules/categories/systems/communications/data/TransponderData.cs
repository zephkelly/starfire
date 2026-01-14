using System;

namespace Starfire.Entity.Modules.Transponder
{
    [Serializable]
    public struct TransponderData
    {
        public string ShipId;
        public FactionData Faction;
        public ShipClassDefinition ShipClass;
        public int CrewComplement;
        public bool IsTransmitting;
        public CommChannel ActiveChannels;

        public bool IsValid => !string.IsNullOrEmpty(ShipId);
        public string ShipClassName => ShipClass != null ? ShipClass.ClassName : "Unknown";
        public string FactionName => Faction != null ? Faction.FactionName : "Unknown";
        public string FactionId => Faction != null ? Faction.FactionId : string.Empty;

        public static TransponderData Empty => new()
        {
            ShipId = string.Empty,
            Faction = null,
            ShipClass = null,
            CrewComplement = 0,
            IsTransmitting = false,
            ActiveChannels = CommChannel.None
        };
    }
}
