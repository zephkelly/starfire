using System;

namespace Starfire.Entity.Modules.Transponder
{
    public class BasicTransponderModule : ITransponderModule
    {
        private readonly BasicTransponderConfig _config;
        private EntityControllerBase _controller;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public string ShipId { get; }
        public string Faction { get; }
        public bool IsTransmitting { get; set; } = true;

        public BasicTransponderModule(BasicTransponderConfig config)
        {
            _config = config;
            ShipId = Guid.NewGuid().ToString()[..8];
            Faction = config.DefaultFaction;
        }

        public void OnAttach(EntityControllerBase controller)
        {
            _controller = controller;
        }

        public void OnDetach()
        {
            _controller = null;
        }

        public void OnUpdate(float deltaTime) { }
    }
}
