namespace Starfire.Entity.Modules.Sensor
{
    public class BasicSensorModule : ISensorModule
    {
        private readonly BasicSensorConfig _config;
        private EntityController _controller;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public float DetectionRange => _config.DetectionRange;
        public float TargetingAccuracy => _config.TargetingAccuracy;

        public BasicSensorModule(BasicSensorConfig config)
        {
            _config = config;
        }

        public void OnAttach(EntityController controller)
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
