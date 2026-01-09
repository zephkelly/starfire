namespace Starfire.Entity.Modules.LifeSupport
{
    public class BasicLifeSupportModule : ILifeSupportModule
    {
        private readonly BasicLifeSupportConfig _config;
        private EntityController _controller;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public int CrewCapacity => _config.CrewCapacity;

        public BasicLifeSupportModule(BasicLifeSupportConfig config)
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
