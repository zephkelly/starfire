namespace Starfire.Entity.Modules.Transponder
{
    public interface ITransponderModule : IEntityModule
    {
        string ShipId { get; }
        FactionData Faction { get; }
        ShipClassDefinition ShipClass { get; }

        bool IsTransmitting { get; set; }
        CommChannel ActiveChannels { get; set; }
        CommChannel MonitoredChannels { get; }

        int CrewComplement { get; set; }

        TransponderData GetTransponderData();

        void SetChannelActive(CommChannel channel, bool active);
        bool IsChannelActive(CommChannel channel);
        bool IsMonitoringChannel(CommChannel channel);
    }
}
