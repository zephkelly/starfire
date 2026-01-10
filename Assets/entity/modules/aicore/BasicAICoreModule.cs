namespace Starfire.Entity.Modules.AICore
{
    public class BasicAICoreModule : IAICoreModule
    {
        private readonly BasicAICoreConfig _config;
        private EntityControllerBase _controller;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public float ProcessingPower => _config.ProcessingPower;
        public bool IsAutonomous { get; set; }

        public BasicAICoreModule(BasicAICoreConfig config)
        {
            _config = config;
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
