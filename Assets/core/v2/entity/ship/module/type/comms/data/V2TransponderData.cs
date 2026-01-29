using System;
using Starfire.Entity;

namespace StarfireV2
{
    /// <summary>
    /// Data structure containing all transponder broadcast information.
    /// </summary>
    [Serializable]
    public struct V2TransponderData
    {
        /// <summary>Unique identifier for this ship.</summary>
        public string ShipId;

        /// <summary>The faction this ship belongs to.</summary>
        public V2FactionData Faction;

        /// <summary>The class definition of this ship.</summary>
        public ShipClassDefinition ShipClass;

        /// <summary>Number of crew members aboard.</summary>
        public int CrewComplement;

        /// <summary>Whether the transponder is actively broadcasting.</summary>
        public bool IsTransmitting;

        /// <summary>Which communication channels are currently active.</summary>
        public V2CommChannel ActiveChannels;

        /// <summary>Returns true if this transponder data represents a valid ship.</summary>
        public bool IsValid => !string.IsNullOrEmpty(ShipId);

        /// <summary>Display name of the ship class, or "Unknown" if not set.</summary>
        public string ShipClassName => ShipClass != null ? ShipClass.ClassName : "Unknown";

        /// <summary>Display name of the faction, or "Unknown" if not set.</summary>
        public string FactionName => Faction != null ? Faction.FactionName : "Unknown";

        /// <summary>Unique ID of the faction, or empty string if not set.</summary>
        public string FactionId => Faction != null ? Faction.FactionId : string.Empty;

        /// <summary>Returns an empty transponder data structure.</summary>
        public static V2TransponderData Empty => new()
        {
            ShipId = string.Empty,
            Faction = null,
            ShipClass = null,
            CrewComplement = 0,
            IsTransmitting = false,
            ActiveChannels = V2CommChannel.None
        };
    }
}
