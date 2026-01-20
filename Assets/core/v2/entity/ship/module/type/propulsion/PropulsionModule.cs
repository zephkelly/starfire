namespace StarfireV2
{
    public class PropulsionModule : IShipPropulsionModule
    {
        private readonly PropulsionModuleConfig _config;
        private IEntityController _controller;

        public ShipModuleCategory Category => ShipModuleCategory.Propulsion;
        public ShipModuleType Type => ShipModuleType.ManeuveringThrusters;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public bool IsEnabled { get; set; } = true;

        public float MaxSpeed => _config.MaxSpeed;
        public float Acceleration => _config.Acceleration;
        public float Drag => _config.Drag;

        public PropulsionModule(PropulsionModuleConfig config)
        {
            _config = config;
        }

        public void OnAttach(IEntityController controller)
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
