namespace Starfire.Entity.Modules.Deflector
{
    public class BasicDeflectorModule : IDeflectorModule
    {
        private readonly BasicDeflectorConfig _config;
        private EntityControllerBase _controller;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public float DeflectionStrength => _config.DeflectionStrength;
        public float BeamFocusMultiplier => _config.BeamFocusMultiplier;

        public BasicDeflectorModule(BasicDeflectorConfig config)
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
