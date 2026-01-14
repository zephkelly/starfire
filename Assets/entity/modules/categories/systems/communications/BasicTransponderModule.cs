using System;

namespace Starfire.Entity.Modules.Transponder
{
    public class BasicTransponderModule : ITransponderModule
    {
        private readonly BasicTransponderConfig _config;
        private EntityControllerBase _controller;
        private ShipClassDefinition _runtimeShipClass;
        private CommChannel _activeChannels;
        private int _crewComplement;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public string ShipId { get; }
        public FactionData Faction => _config.DefaultFaction;
        public ShipClassDefinition ShipClass => _runtimeShipClass ?? _config.ShipClass;

        public bool IsTransmitting { get; set; } = true;
        public CommChannel ActiveChannels
        {
            get => _activeChannels;
            set => _activeChannels = value;
        }
        public CommChannel MonitoredChannels => _config.MonitoredChannels;

        public int CrewComplement
        {
            get => _crewComplement;
            set => _crewComplement = value;
        }

        public BasicTransponderModule(BasicTransponderConfig config)
        {
            _config = config;
            ShipId = Guid.NewGuid().ToString()[..8];
            _activeChannels = config.DefaultActiveChannels;
            _crewComplement = config.DefaultCrewComplement;
        }

        public void OnAttach(EntityControllerBase controller)
        {
            _controller = controller;

            if (controller.Entity is Ship ship)
            {
                _runtimeShipClass = ship.ClassDefinition;
            }
        }

        public void OnDetach()
        {
            _controller = null;
            _runtimeShipClass = null;
        }

        public void OnUpdate(float deltaTime) { }

        public TransponderData GetTransponderData()
        {
            return new TransponderData
            {
                ShipId = ShipId,
                Faction = Faction,
                ShipClass = ShipClass,
                CrewComplement = CrewComplement,
                IsTransmitting = IsTransmitting,
                ActiveChannels = ActiveChannels
            };
        }

        public void SetChannelActive(CommChannel channel, bool active)
        {
            if (active)
                _activeChannels |= channel;
            else
                _activeChannels &= ~channel;
        }

        public bool IsChannelActive(CommChannel channel)
        {
            return (_activeChannels & channel) == channel;
        }

        public bool IsMonitoringChannel(CommChannel channel)
        {
            return (MonitoredChannels & channel) == channel;
        }
    }
}
