namespace StarfireV2
{
    public class PropulsionModule : IShipPropulsionModule
    {
        private readonly PropulsionModuleData _data;
        private IEntityController _controller;

        public ShipModuleCategory Category => ShipModuleCategory.Propulsion;
        public ShipModuleType Type => ShipModuleType.ManeuveringThrusters;

        public string ModuleId => _data.moduleId;
        public string DisplayName => _data.displayName;
        public bool IsEnabled { get; set; } = true;

        public float MaxSpeed => _data.maxSpeed;
        public float Acceleration => _data.acceleration;
        public float Drag => _data.drag;

        public PropulsionModule(PropulsionModuleData data)
        {
            _data = data;
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
