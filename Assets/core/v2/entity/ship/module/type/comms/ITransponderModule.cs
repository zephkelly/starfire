using Starfire.Entity;

namespace StarfireV2
{
    /// <summary>
    /// Interface for transponder modules that broadcast ship identity information.
    /// Transponders allow ships to be identified by sensors and communicate on channels.
    /// </summary>
    public interface ITransponderModule : IShipModule
    {
        /// <summary>Unique identifier for this ship instance.</summary>
        string ShipId { get; }

        /// <summary>The faction this ship belongs to.</summary>
        V2FactionData Faction { get; }

        /// <summary>The class definition of this ship.</summary>
        ShipClassDefinition ShipClass { get; }

        /// <summary>Whether the transponder is actively broadcasting identity.</summary>
        bool IsTransmitting { get; set; }

        /// <summary>Which communication channels are currently active for broadcasting.</summary>
        V2CommChannel ActiveChannels { get; set; }

        /// <summary>Which communication channels this transponder monitors for incoming messages.</summary>
        V2CommChannel MonitoredChannels { get; }

        /// <summary>Number of crew members aboard this ship.</summary>
        int CrewComplement { get; set; }

        /// <summary>Gets the full transponder data for broadcast.</summary>
        V2TransponderData GetTransponderData();

        /// <summary>Sets whether a specific channel is active for broadcasting.</summary>
        void SetChannelActive(V2CommChannel channel, bool active);

        /// <summary>Checks if a specific channel is currently active for broadcasting.</summary>
        bool IsChannelActive(V2CommChannel channel);

        /// <summary>Checks if this transponder is monitoring a specific channel.</summary>
        bool IsMonitoringChannel(V2CommChannel channel);
    }
}
