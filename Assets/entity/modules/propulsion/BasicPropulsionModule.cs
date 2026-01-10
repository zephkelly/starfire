namespace Starfire.Entity.Modules.Propulsion
{
    public class BasicPropulsionModule : IPropulsionModule
    {
        private readonly BasicPropulsionConfig _config;
        private EntityControllerBase _controller;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public float MaxSpeed => _config.MaxSpeed;
        public float Acceleration => _config.Acceleration;
        public float Drag => _config.Drag;

        public BasicPropulsionModule(BasicPropulsionConfig config)
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
