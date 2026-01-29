using System;
using Starfire.Entity;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Implementation of a transponder module that broadcasts ship identity.
    /// </summary>
    public class V2TransponderModule : ITransponderModule
    {
        private readonly V2TransponderModuleConfig _config;
        private IEntityController _controller;
        private readonly string _shipId;
        private V2CommChannel _activeChannels;
        private int _crewComplement;
        private bool _isTransmitting;

        // IEntityModule
        public string ModuleId => _config.ModuleId;
        public bool IsEnabled { get; set; } = true;

        // IShipModule
        public ShipModuleCategory Category => ShipModuleCategory.Comms;
        public ShipModuleType Type => ShipModuleType.Transponder;

        // ITransponderModule
        public string ShipId => _shipId;
        public V2FactionData Faction => _config.DefaultFaction;
        public ShipClassDefinition ShipClass => _config.ShipClass;

        public bool IsTransmitting
        {
            get => _isTransmitting;
            set => _isTransmitting = value;
        }

        public V2CommChannel ActiveChannels
        {
            get => _activeChannels;
            set => _activeChannels = value;
        }

        public V2CommChannel MonitoredChannels => _config.MonitoredChannels;

        public int CrewComplement
        {
            get => _crewComplement;
            set => _crewComplement = Mathf.Max(0, value);
        }

        public V2TransponderModule(V2TransponderModuleConfig config)
        {
            _config = config;
            _shipId = GenerateShipId();
            _activeChannels = config.DefaultActiveChannels;
            _crewComplement = config.DefaultCrewComplement;
            _isTransmitting = true; // Transponders default to transmitting
        }

        public void OnAttach(IEntityController controller)
        {
            _controller = controller;
        }

        public void OnDetach()
        {
            _controller = null;
        }

        public void OnUpdate(float deltaTime)
        {
            // Transponder is passive - no update logic needed
        }

        public V2TransponderData GetTransponderData()
        {
            return new V2TransponderData
            {
                ShipId = _shipId,
                Faction = Faction,
                ShipClass = ShipClass,
                CrewComplement = _crewComplement,
                IsTransmitting = _isTransmitting,
                ActiveChannels = _activeChannels
            };
        }

        public void SetChannelActive(V2CommChannel channel, bool active)
        {
            if (active)
            {
                _activeChannels |= channel;
            }
            else
            {
                _activeChannels &= ~channel;
            }
        }

        public bool IsChannelActive(V2CommChannel channel)
        {
            return (_activeChannels & channel) != 0;
        }

        public bool IsMonitoringChannel(V2CommChannel channel)
        {
            return (MonitoredChannels & channel) != 0;
        }

        private static string GenerateShipId()
        {
            return Guid.NewGuid().ToString()[..8].ToUpperInvariant();
        }
    }
}
